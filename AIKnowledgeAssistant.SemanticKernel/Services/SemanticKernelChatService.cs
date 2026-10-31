using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Data;
using AIKnowledgeAssistant.Plugins;
using AIKnowledgeAssistant.Rag;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;

namespace AIKnowledgeAssistant.Services;

// Semantic Kernel version of the chat service. It does the same job as ManualToolCallingChatService,
// but SK owns the tool-calling loop:
//   Manual: we check FinishReason.ToolCalls, run the tool, append the result, call the model again.
//   SK:     FunctionChoiceBehavior.Auto() does all of that inside one GetChatMessageContentAsync call.
// Nothing about the rules changed: the model only REQUESTS a function, our C# code EXECUTES it.
public sealed class SemanticKernelChatService
{
    // Kernel = the container that holds AI services, plugins and filters.
    private readonly Kernel _kernel;

    // SK's abstraction over the chat model (Azure OpenAI here). Swapping providers means changing only the builder call.
    private readonly IChatCompletionService _chatCompletion;

    // Conversation memory: system + user + assistant (+ tool) messages. We resend it on every call because
    // the model itself is stateless.
    private readonly ChatHistory _chatHistory;

    // Per-request model settings; here it enables automatic function calling.
    private readonly OpenAIPromptExecutionSettings _executionSettings;

    // Milestone 5: chunks + embeddings + vector store behind the SearchDocuments tool.
    private readonly DocumentSearchService _documentSearch;

    public SemanticKernelChatService(AiSettings settings, string systemPrompt)
    {
        settings.Normalize();

        // Fail early with a clear message instead of a confusing HTTP error later.
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ApiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Endpoint);

        DeploymentName = settings.Model;
        ResolvedApiEndpoint = AzureEndpointHelper.NormalizeAzureEndpoint(settings.Endpoint).ToString();
         
        // SK's Azure connector wants only the resource root (https://<name>.openai.azure.com/),
        // so we strip any extra path the user may have pasted into the config.
        string azureResourceEndpoint = AzureEndpointHelper.ToAzureResourceEndpoint(settings.Endpoint);

        // Same in-memory data store as the Manual project (shared via AIKnowledgeAssistant.Core).
        var taskStore = new InMemoryTaskStore();

        // Step 1 - register the AI service. "Model" is the Azure DEPLOYMENT name, not the base model name.
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: settings.Model,
            endpoint: azureResourceEndpoint,
            apiKey: settings.ApiKey);

        // Step 2 - register the filter. SK discovers IFunctionInvocationFilter services from DI automatically,
        // so it wraps every function call (logging + write confirmation).
        builder.Services.AddSingleton<IFunctionInvocationFilter, SkToolVisibilityFilter>();

        // Step 3 - register the tools. SK reads [KernelFunction]/[Description] attributes and builds the JSON
        // tool schema sent to the model (the part we wrote manually in Milestone 2).
        builder.Plugins.AddFromObject(new TaskManagementPlugin(taskStore), pluginName: "TaskManagement");

        // Milestone 5 (RAG) - register a SECOND Azure deployment: the embedding model (text -> vector).
        // Same resource and key as the chat model, different deployment name.
        // SKEXP0010 = SK marks this (recommended, non-obsolete) API as experimental; we accept that for this one call.
#pragma warning disable SKEXP0010
        builder.AddAzureOpenAIEmbeddingGenerator(
            deploymentName: settings.EmbeddingModel,
            endpoint: azureResourceEndpoint,
            apiKey: settings.ApiKey);
#pragma warning restore SKEXP0010

        // Build the kernel first so we can resolve the embedding service, then add the document plugin.
        _kernel = builder.Build();

        // Document search = embeddings + our own vector store. Exposed to the model as the SearchDocuments tool.
        _documentSearch = new DocumentSearchService(_kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
        _kernel.Plugins.AddFromObject(new DocumentPlugin(_documentSearch), pluginName: "Documents");

        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // The system message goes in first and stays at the start of the history for the whole session.
        _chatHistory = new ChatHistory(systemPrompt);

        _executionSettings = new OpenAIPromptExecutionSettings
        {
            // Auto() = advertise all kernel functions to the model AND let SK invoke them automatically
            // when requested. Other options exist (e.g. autoInvoke: false) to get back the manual loop.
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
    }

    // Milestone 5: RAG indexing is async (it calls Azure), so it cannot run in the constructor.
    // Program.cs calls this once at startup, before the first question.
    public Task IndexDocumentsAsync(string folder, CancellationToken cancellationToken = default) =>
        _documentSearch.IndexFolderAsync(folder, cancellationToken);

    public int IndexedChunkCount => _documentSearch.ChunkCount;

    public string DeploymentName { get; }

    public string ResolvedApiEndpoint { get; }

    public async Task<string> SendUserMessageAsync(string userText, CancellationToken cancellationToken = default)
    {
        _chatHistory.AddUserMessage(userText);

        // One call, many possible round trips. Behind the scenes SK does:
        //   send history + tool schemas -> model replies "call function X(args)"
        //   -> filter runs (log + confirm) -> plugin method runs -> result appended to history
        //   -> model is called again -> ... until the model returns plain text.
        // The kernel argument is what gives SK access to the registered plugins and filters.
        ChatMessageContent response = await _chatCompletion.GetChatMessageContentAsync(
            _chatHistory,
            executionSettings: _executionSettings,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        // Store the final assistant answer so the next question has full context.
        _chatHistory.Add(response);
        return response.Content ?? string.Empty;
    }
}
