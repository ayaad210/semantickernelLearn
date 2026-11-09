using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Services;
using Microsoft.Extensions.Configuration;

// ============================================================
// Program.cs = the starting point. It does 4 things:
//   1. Reads the settings (endpoint, key, model names)
//   2. Creates the chat service (all Semantic Kernel code is inside it)
//   3. Indexes the documents (the "R" in RAG: prepare the search)
//   4. Runs a loop: read a question -> get an answer -> print it
// ============================================================

// The system prompt = the rules we give the model before the user says anything.
// It tells the model what tools exist and when to use them.
const string SystemPrompt =
    """
    You are a helpful assistant with these tools:
    - Task tools (ListTasks, CreateTask, CompleteTask) to read and change tasks.
    - SearchDocuments to search local company documents (leave policy, company rules, technical notes).
    - CalculateRemainingDays to subtract used days from a total.
    For questions about company policies or documents, ALWAYS call SearchDocuments first and answer ONLY from
    the returned text, mentioning the source file. If the result is NOT_FOUND or does not contain the answer,
    say clearly that it was not found in the documents. Never guess policy details.
    Some questions need several steps. Example: "I used 5 leave days, how many remain?"
    Step 1: call SearchDocuments to find the annual leave balance.
    Step 2: call CalculateRemainingDays with that balance and the days used. Do not do the math yourself.
    Step 3: answer with the result and the source file.
    After tool results, answer in clear natural language.
    """;

// Step 1: read settings from appsettings.json.
// Environment variables are added last, so they can override the file (useful to keep keys out of the code).
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

// Turn the "Ai" section of the file into an AiSettings object.
var settings = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>()
    ?? throw new InvalidOperationException($"Missing '{AiSettings.SectionName}' section in appsettings.json.");

// Stop early with a clear message if the connection details are missing.
if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.Endpoint))
{
    throw new InvalidOperationException("Set Ai:Endpoint and Ai:ApiKey in appsettings.json.");
}

// Step 2: create the service that talks to the model.
var chatService = new SemanticKernelChatService(settings, SystemPrompt);

// Step 3: RAG indexing. It runs once at startup:
//   read files -> cut into chunks -> turn each chunk into numbers (embedding) -> keep them in memory.
Console.WriteLine("Indexing documents...");
await chatService.IndexDocumentsAsync(Path.Combine(AppContext.BaseDirectory, "Documents"));
Console.WriteLine($"Indexed {chatService.IndexedChunkCount} chunks.");
Console.WriteLine();
Console.WriteLine("Semantic Kernel + RAG demo");
Console.WriteLine("Try: 'List tasks' | 'Create task Review PR' | 'How many annual leave days do I get?'");
Console.WriteLine("Multi-step: 'Read the leave policy and tell me how many days remain if I used 5 days.'");
Console.WriteLine("Type 'exit' to quit.");

// Step 4: the chat loop. It repeats until the user types "exit".
while (true)
{
    Console.Write("\nYou: ");
    string? input = Console.ReadLine();

    // Ignore empty lines.
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        // Send the question. If the model uses a tool, FunctionCallLogger prints
        // [tool call] and [tool result] lines here, before the final answer.
        string reply = await chatService.SendUserMessageAsync(input);
        Console.WriteLine($"\nAssistant: {reply}");
    }
    catch (Exception ex)
    {
        // Typical causes: wrong key, wrong deployment name, no network.
        Console.WriteLine($"\nError: {ex.Message}");
    }
}
