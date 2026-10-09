using AIKnowledgeAssistant.Tools;
using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Services;

// A function invocation filter is SK's "middleware" around every plugin call, similar to ASP.NET middleware.
// It runs AFTER the model has asked for a function and BEFORE the C# method executes.
// We use it for two learning goals:
//   1. Visibility: print what the model requested (the Manual project printed the raw tool call itself;
//      with SK the loop is hidden, so this filter lets us "see behind the abstraction").
//   2. Safety: ask the user to approve write tools (the Milestone 3 confirmation step, now reusable).
//
// WHEN is it called, and BY WHOM?
//   Registered by : SemanticKernelChatService constructor (builder.Services.AddSingleton<IFunctionInvocationFilter, ...>)
//   Called by     : Semantic Kernel itself, NOT by our code. Our code never writes "filter.OnFunctionInvocationAsync(...)".
//   Called when   : once for EVERY tool call, inside SemanticKernelChatService.SendUserMessageAsync ->
//                   _chatCompletion.GetChatMessageContentAsync(...), i.e. after the model replies "call function X"
//                   and before TaskManagementPlugin's method X runs.
//   If the model answers with plain text and requests no tool, the filter is never called.
//   If the model requests 2 tools in one turn, it is called twice (once per tool).
//
// Every Console line below is annotated:  [printed by: <method>, called by: <caller>]
public sealed class SkToolVisibilityFilter : IFunctionInvocationFilter
{
    // Entry point. Called by: Semantic Kernel's function-invocation pipeline (inside GetChatMessageContentAsync).
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        // --- 1. Show the model's request: which function, which plugin, which arguments. ---
        // [printed by: SkToolVisibilityFilter.OnFunctionInvocationAsync, called by: Semantic Kernel after the model requested a tool]
        Console.WriteLine();
        // [same method/caller] banner: marks the start of one tool call
        Console.WriteLine("--- SK auto function call (model requested via FunctionChoiceBehavior.Auto) ---");
        // [same method/caller] tool name the MODEL chose (e.g. ListTasks)
        Console.WriteLine($"Function: {context.Function.Name}");
        // [same method/caller] plugin that owns the tool ("TaskManagement", set in SemanticKernelChatService)
        Console.WriteLine($"Plugin: {context.Function.PluginName}");
        // [same method/caller] header for the arguments the MODEL supplied
        Console.WriteLine("Arguments:");
        foreach (KeyValuePair<string, object?> argument in context.Arguments)
        {
            // SK adds internal entries to the argument bag; they are not model-supplied arguments.
            if (argument.Key is "execution_settings" or "kernel")
            {
                continue;
            }

            // [same method/caller] one line per model-supplied argument, e.g. "title = Review PR"
            Console.WriteLine($"  {argument.Key} = {argument.Value}");
        }

        // --- 2. Permission check: only write tools need explicit user approval. ---
        if (TaskToolNames.RequiresUserConfirmation(context.Function.Name))
        {
            // The next 3 prints + ReadLine only happen for WRITE tools (CreateTask, CompleteTask).
            // [printed by: OnFunctionInvocationAsync, called by: Semantic Kernel] blank spacer line
            Console.WriteLine();
            // [same] warning that this tool changes data; decided by TaskToolNames.RequiresUserConfirmation (Core project)
            Console.WriteLine("*** WRITE tool — this would change in-memory data ***");
            // [same] approval prompt; the program blocks here until the user types
            Console.Write("Approve and execute in C#? (y/n): ");
            // [same] reads the user's answer. This is the "user confirmation" step; the model is NOT involved.
            string? answer = Console.ReadLine();
            if (!IsYes(answer))
            {
                // Denied: we do NOT call next(), so the plugin method never runs.
                // Instead we set a fake result. SK sends this text back to the model as the tool result,
                // so the model can tell the user "it was not done" (the system prompt tells it not to lie).
                const string denied = "User denied the operation. Data was not changed.";
                // [printed by: OnFunctionInvocationAsync (deny branch), called by: Semantic Kernel] only when user typed anything but y/yes
                Console.WriteLine(denied);
                // [same] spacer line; after this the method returns, so the "invoking" and "result" prints below are skipped
                Console.WriteLine();
                context.Result = new FunctionResult(context.Function, denied);
                return;
            }
        }

        // --- 3. Execute: next() runs the next filter or, finally, the real C# plugin method. ---
        // This is the moment "the application executes the operation" — never the model.
        // [printed by: OnFunctionInvocationAsync, called by: Semantic Kernel] printed just BEFORE next(); read tools always reach here,
        // write tools only after approval
        Console.WriteLine("--- SK invoking C# plugin method now ---");
        await next(context);

        // After next() the result is available; SK will send it back to the model automatically.
        // [printed by: OnFunctionInvocationAsync, called by: Semantic Kernel] the string returned by the TaskManagementPlugin method
        // (which got it from InMemoryTaskStore). This is what SK sends back to the model next.
        Console.WriteLine($"Function result: {context.Result.GetValue<object?>()}");
        // [same] spacer line
        Console.WriteLine();
    }

    // Accept only an explicit "y" or "yes"; anything else (including empty input) counts as "no" (safe default).
    private static bool IsYes(string? input) =>
        input?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true ||
        input?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) == true;
}
