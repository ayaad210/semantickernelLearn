using System.Text;
using AIKnowledgeAssistant.Models;

namespace AIKnowledgeAssistant.Data;

// A fake database: the tasks live in a list in memory, so the demo needs no setup.
// They reset every time the app starts.
// The plugin methods call these methods; they return plain text for the model to read.
public sealed class InMemoryTaskStore
{
    // Three starting tasks so "List tasks" shows something.
    private readonly List<TaskItem> _tasks =
    [
        new(1, "Fix login bug", Models.TaskStatus.Pending),
        new(2, "Deploy new version", Models.TaskStatus.InProgress),
        new(3, "Update documentation", Models.TaskStatus.Completed)
    ];

    // Returns one line per task, e.g. "1. Fix login bug - Pending".
    public string ListTasks()
    {
        var builder = new StringBuilder();
        foreach (TaskItem task in _tasks)
        {
            builder.AppendLine($"{task.Id}. {task.Title} - {task.Status}");
        }

        return builder.ToString().TrimEnd();
    }

    // Adds a new Pending task with the next free id.
    public string CreateTask(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Error: title cannot be empty.";
        }

        int newId = _tasks.Max(t => t.Id) + 1;
        _tasks.Add(new TaskItem(newId, title.Trim(), Models.TaskStatus.Pending));
        return $"Created task {newId}: {title.Trim()} (Pending)";
    }

    // Marks a task as Completed. Returns an error text if the id does not exist.
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
