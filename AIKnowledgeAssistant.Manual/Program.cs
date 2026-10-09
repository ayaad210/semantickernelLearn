using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Services;
using Microsoft.Extensions.Configuration;

const string SystemPrompt =
    """
    You are a helpful assistant for a .NET learning project.
    Tools: ListTasks, CalculateRemainingDays (read-only).
    CreateTask and CompleteTask change data — the user must approve before they run.
    If a tool result says the user denied, do not claim the change happened.
    After tool results, answer in clear natural language.
    """;

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

var chatService = new ManualToolCallingChatService(aiSettings, SystemPrompt);

Console.WriteLine("AI Tool-Using Knowledge Assistant");
Console.WriteLine("Project: AIKnowledgeAssistant.Manual (OpenAI SDK + hand-written tool loop)");
Console.WriteLine($"Deployment: {chatService.DeploymentName}");
Console.WriteLine($"API base URL: {chatService.ResolvedApiEndpoint}");
Console.WriteLine("Shared store & settings: AIKnowledgeAssistant.Core");
Console.WriteLine("Try: 'List tasks' | 'Create task Review PR' | 'Complete task 1'");
Console.WriteLine("Type 'exit' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("You: ");
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
        string assistantReply = await chatService.SendUserMessageAsync(userInput);
        Console.WriteLine();
        Console.WriteLine($"Assistant: {assistantReply}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye.");
