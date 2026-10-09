namespace AIKnowledgeAssistant.Models;

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
