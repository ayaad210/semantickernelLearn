using System.ComponentModel;
using AIKnowledgeAssistant.Data;
using AIKnowledgeAssistant.Tools;
using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Plugins;

// A Semantic Kernel "plugin" is just a normal C# class whose methods are exposed to the model as tools.
// Compare with the Manual project: there we wrote JSON tool definitions (LearningToolDefinitions) AND a
// switch statement (LearningToolExecutor) by hand. Here SK builds both from the attributes below:
//   [KernelFunction] -> "this method is a tool"  (tool name)
//   [Description]    -> text the model reads to decide WHEN to call it and WHAT each argument means
// The model never runs this code. It only asks for a method by name; SK then invokes it in our process.
public sealed class TaskManagementPlugin
{
    // Shared in-memory data (lives in AIKnowledgeAssistant.Core, same store the Manual project uses).
    private readonly InMemoryTaskStore _store;

    public TaskManagementPlugin(InMemoryTaskStore store)
    {
        _store = store;
    }

    // READ-ONLY tool: no side effects, so no confirmation is needed.
    [KernelFunction(TaskToolNames.ListTasksName)]
    [Description("List all tasks with id, title, and status. Read-only.")]
    public string ListTasks() => _store.ListTasks();

    // READ-ONLY tool with arguments: the model fills totalDays/usedDays from the user's sentence.
    // SK parses the model's JSON arguments and converts them to int before calling this method.
    [KernelFunction(TaskToolNames.CalculateRemainingDaysName)]
    [Description("Calculate remaining days as totalDays minus usedDays. Read-only.")]
    public string CalculateRemainingDays(
        [Description("Total days available")] int totalDays,
        [Description("Days already used")] int usedDays) =>
        _store.CalculateRemainingDays(totalDays, usedDays).ToString();

    // WRITE tool: changes data. The method itself has NO confirmation logic on purpose —
    // SkToolVisibilityFilter intercepts the call and asks the user (y/n) BEFORE this body runs.
    // Keeping the policy in the filter means every write tool is protected in one place.
    [KernelFunction(TaskToolNames.CreateTaskName)]
    [Description("Create a new task. Write operation — user confirmation handled by filter.")]
    public string CreateTask([Description("Task title")] string title) =>
        _store.CreateTask(title);

    // WRITE tool: same rule as CreateTask — protected by the filter, not by this method.
    [KernelFunction(TaskToolNames.CompleteTaskName)]
    [Description("Mark a task Completed by id. Write operation — user confirmation handled by filter.")]
    public string CompleteTask([Description("Task id")] int id) =>
        _store.CompleteTask(id);
}
