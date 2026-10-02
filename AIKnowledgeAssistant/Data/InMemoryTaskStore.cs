using System.Text;
using AIKnowledgeAssistant.Models;

namespace AIKnowledgeAssistant.Data;

// In-memory tasks — no database. The LLM never touches this object directly.
public sealed class InMemoryTaskStore
{
    private readonly List<TaskItem> _tasks =
    [
        new(1, "Fix login bug", Models.TaskStatus.Pending),
        new(2, "Deploy new version", Models.TaskStatus.InProgress),
        new(3, "Update documentation", Models.TaskStatus.Completed)
    ];

    // Called by our C# tool executor when the model requests ListTasks.
    public string ListTasks()
    {
        var builder = new StringBuilder();
        foreach (TaskItem task in _tasks)
        {
            builder.AppendLine($"{task.Id}. {task.Title} - {task.Status}");
        }

        return builder.ToString().TrimEnd();
    }

    // Simple math tool — model passes numbers; we compute in C#.
    public int CalculateRemainingDays(int totalDays, int usedDays) =>
        totalDays - usedDays;
}
