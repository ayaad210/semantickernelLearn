using System.Text.Json;
using AIKnowledgeAssistant.Data;
using OpenAI.Chat;

namespace AIKnowledgeAssistant.Tools;

// =============================================================================
// Tool EXECUTION (what happens after the model requests a tool)
// =============================================================================
//
// ChatToolCall — one tool invocation the model asked for in its last response.
//   - FunctionName:     which tool (e.g. "ListTasks")
//   - FunctionArguments: JSON string (or BinaryData) of parameters — model-generated
//   - Id:               unique id — must be echoed back in ToolChatMessage
//
// Important: ChatToolCall is a REQUEST. This class runs real C# in your app.
// =============================================================================
public sealed class LearningToolExecutor
{
    private readonly InMemoryTaskStore _taskStore = new();

    public string Execute(ChatToolCall toolCall)
    {
        // --- Learning output: see the raw tool-call payload from the model ---
        Console.WriteLine();
        Console.WriteLine("--- Tool call requested by model (not executed yet by Azure) ---");
        Console.WriteLine($"Tool name: {toolCall.FunctionName}");       // from ChatToolCall
        Console.WriteLine($"Tool call id: {toolCall.Id}");               // tie result to this call
        string argumentsText = toolCall.FunctionArguments.ToString();     // JSON args from model
        Console.WriteLine($"Arguments (JSON): {argumentsText}");
        Console.WriteLine("--- Application executing C# method now ---");

        // Map model's FunctionName to our C# methods (whitelist — no arbitrary code).
        string result = toolCall.FunctionName switch
        {
            LearningToolDefinitions.ListTasksName => _taskStore.ListTasks(),
            LearningToolDefinitions.CalculateRemainingDaysName => ExecuteCalculateRemainingDays(argumentsText),
            _ => throw new InvalidOperationException($"Unknown tool: {toolCall.FunctionName}")
        };

        // This string becomes the body of ToolChatMessage sent back to the model.
        Console.WriteLine($"Tool result sent back to model: {result}");
        Console.WriteLine();

        return result;
    }

    // Parse FunctionArguments JSON into ints, then call domain logic.
    // The model can hallucinate bad JSON — production apps validate more strictly.
    private string ExecuteCalculateRemainingDays(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        JsonElement root = document.RootElement;

        int totalDays = root.GetProperty("totalDays").GetInt32();
        int usedDays = root.GetProperty("usedDays").GetInt32();
        int remaining = _taskStore.CalculateRemainingDays(totalDays, usedDays);

        return remaining.ToString();
    }
}
