using OpenAI.Chat;

namespace AIKnowledgeAssistant.Tools;

// ChatTool menu for the model (Manual app). Names match TaskToolNames in Core.
public static class LearningToolDefinitions
{
    public static bool RequiresUserConfirmation(string functionName) =>
        TaskToolNames.RequiresUserConfirmation(functionName);

    public static IReadOnlyList<ChatTool> All { get; } =
    [
        ChatTool.CreateFunctionTool(
            functionName: TaskToolNames.ListTasksName,
            functionDescription: "List all tasks with id, title, and status. Read-only."),

        ChatTool.CreateFunctionTool(
            functionName: TaskToolNames.CalculateRemainingDaysName,
            functionDescription: "Calculate remaining days as totalDays minus usedDays. Read-only.",
            functionParameters: BinaryData.FromBytes("""
                {
                  "type": "object",
                  "properties": {
                    "totalDays": { "type": "integer" },
                    "usedDays": { "type": "integer" }
                  },
                  "required": ["totalDays", "usedDays"]
                }
                """u8.ToArray())),

        ChatTool.CreateFunctionTool(
            functionName: TaskToolNames.CreateTaskName,
            functionDescription: "Create a new task with the given title. Requires user approval before running.",
            functionParameters: BinaryData.FromBytes("""
                {
                  "type": "object",
                  "properties": {
                    "title": { "type": "string", "description": "Task title" }
                  },
                  "required": ["title"]
                }
                """u8.ToArray())),

        ChatTool.CreateFunctionTool(
            functionName: TaskToolNames.CompleteTaskName,
            functionDescription: "Mark a task as Completed by id. Requires user approval before running.",
            functionParameters: BinaryData.FromBytes("""
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "integer", "description": "Task id" }
                  },
                  "required": ["id"]
                }
                """u8.ToArray()))
    ];
}
