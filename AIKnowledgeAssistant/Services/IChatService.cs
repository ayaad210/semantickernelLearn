using AIKnowledgeAssistant.Models;

namespace AIKnowledgeAssistant.Services;

public interface IChatService
{
    string DeploymentName { get; }

    /// <summary>Actual OpenAI v1 base URL the SDK calls (helpful when debugging 401 errors).</summary>
    string ResolvedApiEndpoint { get; }

    Task<string> CompleteChatAsync(
        IReadOnlyList<ChatMessageItem> messages,
        CancellationToken cancellationToken = default);
}
