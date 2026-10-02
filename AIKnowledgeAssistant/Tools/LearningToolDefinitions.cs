using OpenAI.Chat;

namespace AIKnowledgeAssistant.Tools;

// ChatTool definitions — menu for the model. Write tools need user confirmation in the executor.
public static class LearningToolDefinitions
{
    public const string ListTasksName = "ListTasks";
    public const string CalculateRemainingDaysName = "CalculateRemainingDays";
    public const string CreateTaskName = "CreateTask";
    public const string CompleteTaskName = "CompleteTask";

    // Read-only: run immediately. Write: ask user before C# changes data.
    public static bool RequiresUserConfirmation(string functionName) =>
        functionName is CreateTaskName or CompleteTaskName;

    public static IReadOnlyList<ChatTool> All { get; } =
    [
        ChatTool.CreateFunctionTool(
            functionName: ListTasksName,
            functionDescription: "List all tasks with id, title, and status. Read-only."),

        ChatTool.CreateFunctionTool(
            functionName: CalculateRemainingDaysName,
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
            functionName: CreateTaskName,
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
            functionName: CompleteTaskName,
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
