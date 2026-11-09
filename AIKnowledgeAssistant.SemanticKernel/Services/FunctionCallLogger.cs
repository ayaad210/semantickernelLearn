using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Services;

// A "filter" is code that Semantic Kernel runs around every tool call (like middleware).
// We never call it ourselves: Semantic Kernel calls it when the model asks for a tool.
//
// Milestone 6: in a multi-step question this filter runs once PER STEP, so the console
// shows the whole chain, e.g. SearchDocuments first, then CalculateRemainingDays.
//
// This filter only PRINTS what happens, so the audience can see:
//   [tool call]   what the model asked for, and with which arguments
//   [tool result] what our C# method returned
public sealed class FunctionCallLogger : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        // Build "name=value, name=value" from the arguments the model gave.
        // Semantic Kernel adds two internal entries; we hide them.
        string arguments = string.Join(", ", context.Arguments
            .Where(a => a.Key is not ("execution_settings" or "kernel"))
            .Select(a => $"{a.Key}={a.Value}"));

        // Before: the model has asked for this tool.
        Console.WriteLine();
        Console.WriteLine($"[tool call] {context.Function.PluginName}.{context.Function.Name}({arguments})");

        // Run the real C# method (or the next filter).
        await next(context);

        // After: show what the method returned. This text goes back to the model.
        Console.WriteLine($"[tool result] {context.Result.GetValue<object?>()}");
    }
}
