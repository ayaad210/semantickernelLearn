using System.Text;
using AIKnowledgeAssistant.Models;

namespace AIKnowledgeAssistant.Data;

// In-memory tasks. Only C# methods here change data — never the LLM directly.
public sealed class InMemoryTaskStore
{
    private readonly List<TaskItem> _tasks =
    [
        new(1, "Fix login bug", Models.TaskStatus.Pending),
        new(2, "Deploy new version", Models.TaskStatus.InProgress),
        new(3, "Update documentation", Models.TaskStatus.Completed)
    ];

    public string ListTasks()
    {
        var builder = new StringBuilder();
        foreach (TaskItem task in _tasks)
        {
            builder.AppendLine($"{task.Id}. {task.Title} - {task.Status}");
        }

        return builder.ToString().TrimEnd();
    }

    public int CalculateRemainingDays(int totalDays, int usedDays) =>
        totalDays - usedDays;

    // Write operation — Milestone 3 calls this only after user confirms.
    public string CreateTask(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Error: title cannot be empty.";
        }

        int newId = _tasks.Count == 0 ? 1 : _tasks.Max(t => t.Id) + 1;
        _tasks.Add(new TaskItem(newId, title.Trim(), Models.TaskStatus.Pending));
        return $"Created task {newId}: {title.Trim()} (Pending)";
    }

    // Write operation — sets status to Completed.
    public string CompleteTask(int id)
    {
        TaskItem? task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null)
        {
            return $"Error: no task with id {id}.";
        }

        task.Status = Models.TaskStatus.Completed;
        return $"Completed task {id}: {task.Title}";
    }
}
