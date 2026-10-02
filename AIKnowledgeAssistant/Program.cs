// Milestone 2: chat + manual tool calling (ListTasks, CalculateRemainingDays).
//
// Program does NOT reference ChatTool directly — that lives in:
//   LearningToolDefinitions (ChatTool) → ManualToolCallingChatService (loop) → LearningToolExecutor (C#)

using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Services;
using Microsoft.Extensions.Configuration;

const string SystemPrompt =
    """
    You are a helpful assistant for a .NET learning project.
    You have tools: ListTasks and CalculateRemainingDays.
    Use ListTasks when the user asks about tasks.
    Use CalculateRemainingDays for remaining days math.
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

// ManualToolCallingChatService owns ChatClient, ChatCompletionOptions.Tools, and the tool loop.
var chatService = new ManualToolCallingChatService(aiSettings, SystemPrompt);

Console.WriteLine("AI Tool-Using Knowledge Assistant");
Console.WriteLine("Milestone 2: Manual Tool Calling");
Console.WriteLine($"Deployment: {chatService.DeploymentName}");
Console.WriteLine($"API base URL: {chatService.ResolvedApiEndpoint}");
Console.WriteLine("Try: 'List my tasks' or 'If I have 20 days and used 5, how many remain?'");
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
        // May call Azure multiple times if the model returns FinishReason ToolCalls.
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
