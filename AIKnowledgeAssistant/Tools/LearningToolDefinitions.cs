using OpenAI.Chat;

namespace AIKnowledgeAssistant.Tools;

// =============================================================================
// Tool DEFINITIONS (what the model is allowed to request)
// =============================================================================
//
// ChatTool — SDK type that describes one callable function for the model.
//   - Sent to Azure on every chat request inside ChatCompletionOptions.Tools
//   - The model reads name + description + parameter schema
//   - The model does NOT run this code; it only returns a ChatToolCall later
//
// ChatTool.CreateFunctionTool(...) — builds that description from:
//   - functionName:     must match a case in LearningToolExecutor (e.g. "ListTasks")
//   - functionDescription: helps the model decide WHEN to use the tool
//   - functionParameters: JSON Schema for arguments (optional if no args)
//
// K8s analogy: this file is like publishing "allowed operations" (list pods, get logs).
//              It is not running kubectl — just declaring what exists.
// =============================================================================
public static class LearningToolDefinitions
{
    // Use constants so definition names and executor switch stay in sync.
    public const string ListTasksName = "ListTasks";
    public const string CalculateRemainingDaysName = "CalculateRemainingDays";

    // All tools we register with the API for this milestone.
    public static IReadOnlyList<ChatTool> All { get; } =
    [
        // Tool with no parameters — empty object schema is implied when omitted.
        ChatTool.CreateFunctionTool(
            functionName: ListTasksName,
            functionDescription: "List all tasks in the in-memory task store with id, title, and status."),

        // Tool with parameters — model must return JSON matching this schema in FunctionArguments.
        ChatTool.CreateFunctionTool(
            functionName: CalculateRemainingDaysName,
            functionDescription: "Calculate remaining days as totalDays minus usedDays.",
            functionParameters: BinaryData.FromBytes("""
                {
                  "type": "object",
                  "properties": {
                    "totalDays": { "type": "integer", "description": "Total days available" },
                    "usedDays": { "type": "integer", "description": "Days already used" }
                  },
                  "required": ["totalDays", "usedDays"]
                }
                """u8.ToArray()))
    ];
}
