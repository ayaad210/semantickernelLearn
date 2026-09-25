using System.ClientModel;
using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Models;
using OpenAI;
using OpenAI.Chat;

namespace AIKnowledgeAssistant.Services;

// =============================================================================
// AzureOpenAiChatService — Chat Completions with API key (appsettings.json)
// =============================================================================
//
// Microsoft Learn pattern for API key + Chat Completions:
//   https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/chatgpt
//
//   OpenAIClient + ApiKeyCredential + Endpoint ending in /openai/v1/
//
// Foundry portal samples often show Entra ID (DefaultAzureCredential). This project
// intentionally uses API key only, as configured in appsettings.json.
//
// =============================================================================
public sealed class AzureOpenAiChatService : IChatService
{
    private readonly ChatClient _chatClient;

    public AzureOpenAiChatService(AiSettings settings)
    {
        settings.Normalize();

        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ApiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Endpoint);

        DeploymentName = settings.Model;

        Uri endpoint = NormalizeAzureEndpoint(settings.Endpoint);
        ResolvedApiEndpoint = endpoint.ToString();

        // Same construction order as official Azure OpenAI + OpenAI .NET SDK samples.
        OpenAIClient openAiClient = new(
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = endpoint
            });

        _chatClient = openAiClient.GetChatClient(settings.Model);
    }

    public string DeploymentName { get; }

    public string ResolvedApiEndpoint { get; }

    public async Task<string> CompleteChatAsync(
        IReadOnlyList<ChatMessageItem> messages,
        CancellationToken cancellationToken = default)
    {
        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            ToOpenAiMessages(messages),
            cancellationToken: cancellationToken);

        return completion.Content[0].Text;
    }

    /// <summary>
    /// Builds Azure OpenAI v1 base URI for API-key auth.
    /// Prefer *.openai.azure.com (documented for API keys in Microsoft Learn).
    /// </summary>
    internal static Uri NormalizeAzureEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint.Trim());
        string host = uri.Host;

        if (uri.AbsolutePath.Contains("/api/projects/", StringComparison.OrdinalIgnoreCase))
        {
            host = uri.Host;
        }

        // Foundry host → classic OpenAI host (same resource, API-key friendly URL).
        if (host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            string resourceName = host[..^".services.ai.azure.com".Length];
            return new Uri($"https://{resourceName}.openai.azure.com/openai/v1/");
        }

        if (host.EndsWith(".openai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri($"https://{host}/openai/v1/");
        }

        if (host.EndsWith(".cognitiveservices.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri($"https://{host}/openai/v1/");
        }

        var trimmed = endpoint.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(trimmed + "/");
        }

        return new Uri(trimmed + "/openai/v1/");
    }

    private static IEnumerable<ChatMessage> ToOpenAiMessages(IReadOnlyList<ChatMessageItem> messages)
    {
        foreach (ChatMessageItem message in messages)
        {
            yield return message.Role.ToLowerInvariant() switch
            {
                "system" => new SystemChatMessage(message.Content),
                "assistant" => new AssistantChatMessage(message.Content),
                "user" => new UserChatMessage(message.Content),
                _ => throw new InvalidOperationException(
                    $"Unsupported role '{message.Role}'. Use system, user, or assistant.")
            };
        }
    }
}
