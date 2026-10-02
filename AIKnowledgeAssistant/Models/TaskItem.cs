namespace AIKnowledgeAssistant.Models;

// One task row in memory. Status can change when CompleteTask runs (after user approval).
public sealed class TaskItem
{
    public TaskItem(int id, string title, TaskStatus status)
    {
        Id = id;
        Title = title;
        Status = status;
    }

    public int Id { get; }

    public string Title { get; }

    public TaskStatus Status { get; set; }
}
