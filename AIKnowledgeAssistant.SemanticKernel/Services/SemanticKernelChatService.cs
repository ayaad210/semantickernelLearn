using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Data;
using AIKnowledgeAssistant.Plugins;
using AIKnowledgeAssistant.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AIKnowledgeAssistant.Services;

// ============================================================
// This class is where Semantic Kernel is set up and used.
//
// Main idea: the model never runs our code.
//   - The model only SAYS "please call this function with these arguments".
//   - Semantic Kernel runs the C# method and sends the result back to the model.
//   - This repeats until the model has a final text answer.
// ============================================================
public sealed class SemanticKernelChatService
{
    // The Kernel is a container. It holds the AI services, the tools (plugins) and the filter.
    private readonly Kernel _kernel;

    // Used to talk to the chat model.
    private readonly IChatCompletionService _chatCompletion;

    // The conversation so far (system, user, assistant, tool messages).
    // The model has no memory, so we send the whole history every time.
    private readonly ChatHistory _chatHistory;

    // Settings for each request. Here: "let the model call tools automatically".
    private readonly OpenAIPromptExecutionSettings _executionSettings;

    // The RAG part: indexes the documents and searches them.
    private readonly DocumentSearchService _documentSearch;

    public SemanticKernelChatService(AiSettings settings, string systemPrompt)
    {
        // Semantic Kernel wants only the base address, like https://name.openai.azure.com/
        string endpoint = AzureEndpointHelper.ToResourceEndpoint(settings.Endpoint!);

        var builder = Kernel.CreateBuilder();

        // 1. Add the chat model. "Model" is the deployment name you created in Azure.
        builder.AddAzureOpenAIChatCompletion(settings.Model, endpoint, settings.ApiKey!);

        // 2. Add the embedding model. RAG uses it to turn text into numbers.
        //    It is a second deployment in the same Azure resource.
        //    (SKEXP0010 = Semantic Kernel marks this API "experimental"; we accept that.)
#pragma warning disable SKEXP0010
        builder.AddAzureOpenAIEmbeddingGenerator(settings.EmbeddingModel, endpoint, settings.ApiKey!);
#pragma warning restore SKEXP0010

        // 3. Add the logger filter. Semantic Kernel finds it by itself and runs it around every tool call.
        builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionCallLogger>();

        // 4. Add the task tools. Semantic Kernel reads the [KernelFunction] and [Description]
        //    attributes and tells the model which tools exist.
        builder.Plugins.AddFromObject(new TaskManagementPlugin(new InMemoryTaskStore()), "TaskManagement");

        // Milestone 6: a calculator tool. The model can combine it with SearchDocuments in ONE question
        // (search the policy first, then calculate), because Semantic Kernel keeps looping
        // until the model stops asking for tools.
        builder.Plugins.AddFromObject(new CalculatorPlugin(), "Calculator");

        _kernel = builder.Build();

        // 5. Add the document search tool (RAG).
        //    It needs the embedding service, which exists only after Build(), so we add it here.
        _documentSearch = new DocumentSearchService(_kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
        _kernel.Plugins.AddFromObject(new DocumentPlugin(_documentSearch), "Documents");

        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // The history starts with the system prompt (the rules for the model).
        _chatHistory = new ChatHistory(systemPrompt);

        // Auto = tell the model about all tools AND run them for the model as soon as it asks.
        // There is no "are you sure?" step: the tool runs directly.
        _executionSettings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
    }

    // How many chunks were indexed (printed at startup).
    public int IndexedChunkCount => _documentSearch.ChunkCount;

    // Indexing calls Azure (embeddings), so it is async and cannot run inside the constructor.
    public Task IndexDocumentsAsync(string folder, CancellationToken cancellationToken = default) =>
        _documentSearch.IndexFolderAsync(folder, cancellationToken);

    // Sends one user message and returns the model's final text answer.
    public async Task<string> SendUserMessageAsync(string userText, CancellationToken cancellationToken = default)
    {
        _chatHistory.AddUserMessage(userText);

        // This ONE call hides a loop:
        //   send history + tool list -> model says "call tool X"
        //   -> filter logs it -> our C# method runs -> result is added to the history
        //   -> model is called again -> ... until the model answers with plain text.
        ChatMessageContent response = await _chatCompletion.GetChatMessageContentAsync(
            _chatHistory, _executionSettings, _kernel, cancellationToken);

        // Save the answer so the next question has the full conversation.
        _chatHistory.Add(response);
        return response.Content ?? string.Empty;
    }
}
