using System.Text.Json;
using AIKnowledgeAssistant.Data;
using OpenAI.Chat;

namespace AIKnowledgeAssistant.Tools;

public sealed class LearningToolExecutor
{
    private readonly InMemoryTaskStore _taskStore = new();

    public string Execute(ChatToolCall toolCall)
    {
        Console.WriteLine();
        Console.WriteLine("--- Tool call requested by model ---");
        Console.WriteLine($"Tool name: {toolCall.FunctionName}");
        Console.WriteLine($"Tool call id: {toolCall.Id}");
        string argumentsText = toolCall.FunctionArguments.ToString();
        Console.WriteLine($"Arguments (JSON): {argumentsText}");

        if (TaskToolNames.RequiresUserConfirmation(toolCall.FunctionName))
        {
            Console.WriteLine();
            Console.WriteLine("*** WRITE tool — this would change in-memory data ***");
            Console.Write("Approve and execute in C#? (y/n): ");
            string? answer = Console.ReadLine();
            if (!IsYes(answer))
            {
                const string denied = "User denied the operation. Data was not changed.";
                Console.WriteLine(denied);
                Console.WriteLine();
                return denied;
            }
        }

        Console.WriteLine("--- Application executing C# method now ---");

        string result = toolCall.FunctionName switch
        {
            TaskToolNames.ListTasksName => _taskStore.ListTasks(),
            TaskToolNames.CalculateRemainingDaysName => ExecuteCalculateRemainingDays(argumentsText),
            TaskToolNames.CreateTaskName => ExecuteCreateTask(argumentsText),
            TaskToolNames.CompleteTaskName => ExecuteCompleteTask(argumentsText),
            _ => throw new InvalidOperationException($"Unknown tool: {toolCall.FunctionName}")
        };

        Console.WriteLine($"Tool result sent back to model: {result}");
        Console.WriteLine();

        return result;
    }

    private static bool IsYes(string? input) =>
        input?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true ||
        input?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) == true;

    private string ExecuteCalculateRemainingDays(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        JsonElement root = document.RootElement;

        int totalDays = root.GetProperty("totalDays").GetInt32();
        int usedDays = root.GetProperty("usedDays").GetInt32();
        return _taskStore.CalculateRemainingDays(totalDays, usedDays).ToString();
    }

    private string ExecuteCreateTask(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        string title = document.RootElement.GetProperty("title").GetString()
            ?? throw new InvalidOperationException("title is required.");
        return _taskStore.CreateTask(title);
    }

    private string ExecuteCompleteTask(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        int id = document.RootElement.GetProperty("id").GetInt32();
        return _taskStore.CompleteTask(id);
    }
}
