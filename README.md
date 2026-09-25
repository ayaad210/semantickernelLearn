# semanticKernelLearn

Learning project: **AI Tool-Using Knowledge Assistant** — a .NET console app for AI concepts (chat, tool calling, RAG, and more).

## Projects

| Folder | Description |
| --- | --- |
| [AIKnowledgeAssistant](./AIKnowledgeAssistant/) | Main learning app (Milestone 1: basic chat with Azure OpenAI / Foundry) |
| [semanticKernelLearn](./semanticKernelLearn/) | Empty template project (optional) |

## Setup

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Copy `AIKnowledgeAssistant/appsettings.example.json` to `AIKnowledgeAssistant/appsettings.json`.
3. Set your Azure **Endpoint**, **ApiKey**, and deployment **Model** name.
4. Run:

```powershell
cd AIKnowledgeAssistant
dotnet run
```

Do not commit `appsettings.json` (contains API keys). It is listed in `.gitignore`.

## Roadmap

See [notes.txt](./notes.txt) for the full milestone plan (Milestone 2: manual tool calling, etc.).
