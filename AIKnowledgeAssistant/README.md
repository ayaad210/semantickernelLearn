# AI Tool-Using Knowledge Assistant

A small .NET Console learning project for AI concepts: chat, tool calling, RAG, and more.

## Milestone 1 — Basic Chat

This version sends your messages to **Azure OpenAI / Microsoft Foundry** and prints the model response.

## Prerequisites

- .NET 10 SDK
- A Foundry project or Azure OpenAI deployment

## Configuration

Edit `appsettings.json` (copy from `appsettings.example.json` on a new machine):

```json
{
  "Ai": {
    "Model": "gpt-5-mini",
    "Endpoint": "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/YOUR-PROJECT",
    "ApiKey": "YOUR-AZURE-API-KEY"
  }
}
```

Notes:

- `Model` = your **deployment name** in the portal
- `Endpoint` = Foundry **project** URL or classic `https://resource.openai.azure.com/`
- `ApiKey` = key from Azure portal

`appsettings.json` is in `.gitignore` so your key is less likely to be committed. Only `appsettings.example.json` should be shared without real secrets.

Optional override via environment variable: `Ai__ApiKey`, `Ai__Endpoint`, `Ai__Model`.

## Run

```powershell
cd AIKnowledgeAssistant
dotnet run
```

## Troubleshooting

| Error | Likely cause | Fix |
| --- | --- | --- |
| Missing API key | Empty `ApiKey` in appsettings | Set `Ai:ApiKey` in appsettings.json |
| 401 Unauthorized | Wrong key, wrong URL, or key not for this resource | Use **Keys and Endpoint** from the Foundry resource; prefer `https://RESOURCE.services.ai.azure.com/` or `https://RESOURCE.openai.azure.com/` (not always the `/api/projects/...` URL with API keys). Regenerate key if exposed. |
| DeploymentNotFound | Wrong deployment name | Set `Ai:Model` to your deployment name |
