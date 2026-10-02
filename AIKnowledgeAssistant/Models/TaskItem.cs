namespace AIKnowledgeAssistant.Models;

// One row in our fake task-management system (Milestone 2 demo data).
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

    public TaskStatus Status { get; }
}
