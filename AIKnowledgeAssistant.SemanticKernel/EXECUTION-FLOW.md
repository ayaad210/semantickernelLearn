# Execution Flow — Semantic Kernel Project

Who calls what, and who prints each console line, for the user input **`Create task Review PR`**.

**Golden rule:** the model never runs C#. It only *requests* a function. Semantic Kernel (SK) and our code execute it.

---

## 1. Participants

| Participant | File | Role |
| --- | --- | --- |
| **You** | console | Types questions, answers y/n |
| **Program.cs** | `Program.cs` | Reads input, prints `You:` / `Assistant:` / `Error:` |
| **ChatService** | `Services/SemanticKernelChatService.cs` | Holds Kernel + ChatHistory, calls SK once per question |
| **SK** | Semantic Kernel library | Runs the tool-calling loop (`FunctionChoiceBehavior.Auto`) |
| **Azure OpenAI** | cloud | The model: returns a tool request or final text |
| **Filter** | `Services/SkToolVisibilityFilter.cs` | Called **by SK** before every tool: logs + asks y/n for writes |
| **Plugin** | `Plugins/TaskManagementPlugin.cs` | The real C# tool methods |
| **Store** | `Core/Data/InMemoryTaskStore.cs` | The in-memory task list |

---

## 2. Full sequence — write tool, user approves

```mermaid
sequenceDiagram
    autonumber
    actor You
    participant P as Program.cs
    participant CS as ChatService
    participant SK as Semantic Kernel
    participant AI as Azure OpenAI (model)
    participant F as SkToolVisibilityFilter
    participant PL as TaskManagementPlugin
    participant ST as InMemoryTaskStore

    P->>You: prints "You: "
    You->>P: "Create task Review PR"
    P->>CS: SendUserMessageAsync(text)
    CS->>CS: _chatHistory.AddUserMessage(text)
    CS->>SK: GetChatMessageContentAsync(history, Auto, kernel)

    Note over SK,AI: ROUND TRIP 1 - model decides
    SK->>AI: history + tool schemas (ListTasks, CreateTask, ...)
    AI-->>SK: tool request: CreateTask(title="Review PR")
    Note right of AI: Model only REQUESTS.<br/>Nothing has run yet.

    SK->>F: OnFunctionInvocationAsync(context)
    F->>You: prints "--- SK auto function call ---"<br/>Function / Plugin / Arguments
    F->>You: prints "*** WRITE tool ***"<br/>"Approve and execute in C#? (y/n)"
    You->>F: "y"
    F->>You: prints "--- SK invoking C# plugin method now ---"

    F->>PL: await next(context) -> CreateTask("Review PR")
    PL->>ST: CreateTask("Review PR")
    ST-->>PL: "Created task 4"
    PL-->>F: result string
    F->>You: prints "Function result: Created task 4"
    F-->>SK: result

    Note over SK,AI: ROUND TRIP 2 - model writes the answer
    SK->>AI: history + tool result
    AI-->>SK: final text "I created task 4: Review PR"
    SK-->>CS: ChatMessageContent
    CS->>CS: _chatHistory.Add(response)
    CS-->>P: reply string
    P->>You: prints "Assistant: I created task 4: Review PR"
    P->>You: prints "You: " (loop again)
```

---

## 3. User denies the write (types `n`)

```mermaid
sequenceDiagram
    autonumber
    actor You
    participant SK as Semantic Kernel
    participant AI as Azure OpenAI (model)
    participant F as SkToolVisibilityFilter
    participant PL as TaskManagementPlugin

    SK->>AI: history + tool schemas
    AI-->>SK: tool request: CreateTask("Review PR")
    SK->>F: OnFunctionInvocationAsync(context)
    F->>You: prints request + "Approve? (y/n)"
    You->>F: "n"
    F->>You: prints "User denied the operation. Data was not changed."
    F-->>SK: context.Result = denied text (next() NOT called)
    Note over F,PL: Plugin and Store are NEVER executed.<br/>No "invoking" or "Function result" lines.
    SK->>AI: history + "User denied..." as tool result
    AI-->>SK: "I did not create the task."
```

---

## 4. Read-only tool (`List tasks`)

Same as section 2, **minus** the WRITE warning, the y/n prompt and the user's answer:

```mermaid
sequenceDiagram
    autonumber
    participant SK as Semantic Kernel
    participant AI as Azure OpenAI (model)
    participant F as SkToolVisibilityFilter
    participant PL as TaskManagementPlugin

    SK->>AI: history + tool schemas
    AI-->>SK: tool request: ListTasks()
    SK->>F: OnFunctionInvocationAsync(context)
    F->>F: prints request, RequiresUserConfirmation = false
    F->>PL: next(context) -> ListTasks()
    PL-->>F: task list text
    F-->>SK: result (printed as "Function result")
    SK->>AI: history + tool result
    AI-->>SK: final text
```

---

## 5. Decision flow inside the filter

```mermaid
flowchart TD
    A[SK calls OnFunctionInvocationAsync] --> B[Print function, plugin, arguments]
    B --> C{Write tool?<br/>RequiresUserConfirmation}
    C -- No: read-only --> E
    C -- Yes --> D[Print WRITE warning + ask y/n]
    D --> D2{User answer}
    D2 -- n / empty / other --> X[Print denied message<br/>set context.Result<br/>RETURN without next]
    D2 -- y / yes --> E[Print 'invoking C# plugin method']
    E --> F[await next -> Plugin method -> Store]
    F --> G[Print 'Function result']
    G --> H[Return to SK]
    X --> H
    H --> I[SK sends result back to the model]
```

---

## 6. Who prints each console line

| Console output | Printed by | Condition |
| --- | --- | --- |
| Startup banner (`AI Tool-Using Knowledge Assistant`, deployment, URL...) | `Program.cs` | once at startup |
| `You:` | `Program.cs` | every loop |
| `--- SK auto function call ---`, `Function:`, `Plugin:`, `Arguments:` | **Filter** (called by SK) | every tool call |
| `*** WRITE tool ***`, `Approve and execute in C#? (y/n)` | **Filter** | write tools only |
| `User denied the operation...` | **Filter** | answer was not y/yes |
| `--- SK invoking C# plugin method now ---` | **Filter** | read tools, or approved writes |
| `Function result: ...` | **Filter** | after the plugin ran |
| `Assistant: ...` | `Program.cs` | after all tool calls finish |
| `Error: ...` | `Program.cs` | an exception was thrown |
| `Goodbye.` | `Program.cs` | after `exit` |

---

## 7. Key takeaways

- **One line in our code, many steps inside SK.** `GetChatMessageContentAsync` hides the loop we wrote by hand in the Manual project.
- **At least 2 model calls per tool use:** (1) get the tool request, (2) get the final answer. Our C# runs only between them.
- **The filter is called by SK, never by us**, once per tool call. No tool request means no filter call.
- **Confirmation is the application's job, not the model's.** The filter can block execution by not calling `next()`.
- **Kubernetes analogy:** the model is the controller that emits "run job CreateTask". SK is the operator, the filter is the admission webhook (approve/deny), and the plugin is the job that touches the real system.
