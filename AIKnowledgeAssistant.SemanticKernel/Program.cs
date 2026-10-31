using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Services;
using Microsoft.Extensions.Configuration;

// System message: sets the assistant's role and rules. It is the first message in the chat history.
// It tells the model which tools are read-only vs. write, and how to behave when the user denies a write.
const string SystemPrompt =
    """
    You are a helpful assistant for a .NET learning project.
    Tools: ListTasks, CalculateRemainingDays (read-only).
    CreateTask and CompleteTask change data — the user must approve before they run.
    If a tool result says the user denied, do not claim the change happened.
    After tool results, answer in clear natural language.
    SearchDocuments (read-only) searches local company documents (leave policy, company rules, technical notes).
    For questions about company policies or documents, ALWAYS call SearchDocuments first and answer ONLY from
    the returned text, mentioning the source file. If the result is NOT_FOUND or does not contain the answer,
    say clearly that the answer was not found in the documents. Never guess policy details.
    """;

// Configuration: appsettings.json first, environment variables override it (keeps secrets out of source code).
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var aiSettings = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>()
    ?? throw new InvalidOperationException($"Missing '{AiSettings.SectionName}' configuration section.");

aiSettings.Normalize();

if (string.IsNullOrWhiteSpace(aiSettings.ApiKey))
{
    throw new InvalidOperationException("Missing ApiKey in appsettings.json.");
}

// All Semantic Kernel wiring (kernel, plugin, filter, chat history) happens inside this one service.
var chatService = new SemanticKernelChatService(aiSettings, SystemPrompt);

// Every Console line in this file is printed by Program.cs top-level code (our own code, no SK involved).
// Milestone 5 (RAG) indexing: documents -> chunks -> embeddings (Azure call) -> vector store. Runs once at startup.
Console.WriteLine("Indexing documents (chunk + embed)...");                                              // [Program.cs, startup]
await chatService.IndexDocumentsAsync(Path.Combine(AppContext.BaseDirectory, "Documents"));
Console.WriteLine($"Indexed {chatService.IndexedChunkCount} chunks in the in-memory vector store.");       // [Program.cs, startup]
Console.WriteLine();

// Startup banner: printed once, after indexing, before the first "You:" prompt.
Console.WriteLine("AI Tool-Using Knowledge Assistant");                                              // [Program.cs, startup]
Console.WriteLine("Project: AIKnowledgeAssistant.SemanticKernel (plugins + auto function calling)");      // [Program.cs, startup]
Console.WriteLine($"Deployment: {chatService.DeploymentName}");                                          // [Program.cs, startup; value comes from SemanticKernelChatService]
Console.WriteLine($"API base URL: {chatService.ResolvedApiEndpoint}");                                   // [Program.cs, startup; value comes from SemanticKernelChatService]
Console.WriteLine("Shared store & settings: AIKnowledgeAssistant.Core");                                 // [Program.cs, startup]
Console.WriteLine("Compare with: AIKnowledgeAssistant.Manual (hand-written tool loop)");                 // [Program.cs, startup]
Console.WriteLine("Try: 'List tasks' | 'Create task Review PR' | 'Complete task 1' | 'How many annual leave days do I get?'");                   // [Program.cs, startup]
Console.WriteLine("Type 'exit' to quit.");                                                               // [Program.cs, startup]
Console.WriteLine();                                                                                    // [Program.cs, startup spacer]

// Simple REPL: read a question, let the service handle model + tool calls, print the final answer.
// Tool-call details (function, arguments, y/n prompt, result) are printed by SkToolVisibilityFilter in between.
while (true)
{
    // [Program.cs, loop start] prompt shown before every question (also reappears after the tool-call logs + answer)
    Console.Write("You: ");
    // [Program.cs] waits for the user's question; "exit" ends the loop
    string? userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        // While this await runs, SK may call SkToolVisibilityFilter (see its prints: "--- SK auto function call", "Function:", ...).
        // Those lines appear BEFORE the "Assistant:" line below, because the final text only exists after all tool calls finish.
        string assistantReply = await chatService.SendUserMessageAsync(userInput);
        Console.WriteLine();                                       // [Program.cs, after the whole model+tool round finished] spacer
        Console.WriteLine($"Assistant: {assistantReply}");         // [Program.cs] the model's FINAL natural-language answer
        Console.WriteLine();                                       // [Program.cs] spacer
    }
    catch (Exception ex)
    {
        Console.WriteLine();                                       // [Program.cs, only if SendUserMessageAsync threw] spacer
        Console.WriteLine($"Error: {ex.Message}");                 // [Program.cs] e.g. bad API key, wrong deployment name, network error
        Console.WriteLine();                                       // [Program.cs] spacer
    }
}

Console.WriteLine("Goodbye.");                                     // [Program.cs, after typing 'exit']
