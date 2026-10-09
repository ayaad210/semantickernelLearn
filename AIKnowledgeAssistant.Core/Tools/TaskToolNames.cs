        namespace AIKnowledgeAssistant.Tools;

// Same tool names for Manual (ChatTool) and Semantic Kernel ([KernelFunction]).
public static class TaskToolNames
{
    public const string ListTasksName = "ListTasks";
    public const string CalculateRemainingDaysName = "CalculateRemainingDays";
    public const string CreateTaskName = "CreateTask";
    public const string CompleteTaskName = "CompleteTask";

    public static bool RequiresUserConfirmation(string functionName) =>
        functionName is CreateTaskName or CompleteTaskName;
}
