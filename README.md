# semanticKernelLearn

Learning project: **AI Tool-Using Knowledge Assistant** — chat, manual tool calling, and Semantic Kernel in one solution.

## Solution layout

| Project | Run | What it demonstrates |
| --- | --- | --- |
| **AIKnowledgeAssistant.Core** | (library) | Shared `InMemoryTaskStore`, `AiSettings`, `TaskToolNames`, Azure endpoint helpers |
| **AIKnowledgeAssistant.Manual** | `dotnet run --project AIKnowledgeAssistant.Manual` | OpenAI .NET SDK + **your** `FinishReason.ToolCalls` loop |
| **AIKnowledgeAssistant.SemanticKernel** | `dotnet run --project AIKnowledgeAssistant.SemanticKernel` | **Semantic Kernel** plugins + `FunctionChoiceBehavior.Auto()` |

Both console apps use the **same task store and tool names**; only orchestration differs.

Open `semanticKernelLearn.slnx` in Visual Studio / Cursor.

## Setup

1. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Copy `appsettings.example.json` → `appsettings.json` in **each** app folder you run (Manual and/or SemanticKernel), or copy one file to both.
3. Set Azure **Endpoint**, **ApiKey**, and deployment **Model**.

```powershell
cd c:\Users\mohamed.saied\source\repos\semanticKernelLearn

dotnet run --project AIKnowledgeAssistant.Manual
# or
dotnet run --project AIKnowledgeAssistant.SemanticKernel
```

Do not commit `appsettings.json` (gitignored).

## Docs

- [MILESTONE4-SCENARIOS.md](./AIKnowledgeAssistant.SemanticKernel/MILESTONE4-SCENARIOS.md) — SK flow diagrams (Manual is the other project in the same solution)
- [EXECUTION-FLOW.md](./AIKnowledgeAssistant.SemanticKernel/EXECUTION-FLOW.md) — who calls what / who prints each console line
- [MILESTONE5-RAG-STEPS.md](./AIKnowledgeAssistant.SemanticKernel/MILESTONE5-RAG-STEPS.md) — RAG: ordered steps, changes, concepts (SemanticKernel project; Milestone 4 state tagged `milestone-4-semantic-kernel`)
- [notes.txt](./notes.txt) — full milestone roadmap
