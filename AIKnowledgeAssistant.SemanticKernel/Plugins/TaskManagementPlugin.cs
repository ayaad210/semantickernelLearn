using System.ComponentModel;
using AIKnowledgeAssistant.Data;
using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Plugins;

// A plugin is a normal C# class. Its methods become tools the model can ask for.
//   [KernelFunction] = "this method is a tool" (the string is the tool name)
//   [Description]    = what the model reads to decide WHEN to use the tool
//                      and what each argument means
//
// The model never runs these methods. It only asks, and Semantic Kernel runs them in our app.
public sealed class TaskManagementPlugin(InMemoryTaskStore store)
{
    // Tool 1: read the tasks. No arguments.
    [KernelFunction("ListTasks")]
    [Description("List all tasks with id, title and status.")]
    public string ListTasks() => store.ListTasks();

    // Tool 2: add a task. The model gets the title from the user's sentence.
    [KernelFunction("CreateTask")]
    [Description("Create a new task.")]
    public string CreateTask([Description("Task title")] string title) => store.CreateTask(title);

    // Tool 3: finish a task. The model gets the id from the user's sentence.
    [KernelFunction("CompleteTask")]
    [Description("Mark a task as completed by id.")]
    public string CompleteTask([Description("Task id")] int id) => store.CompleteTask(id);
}
