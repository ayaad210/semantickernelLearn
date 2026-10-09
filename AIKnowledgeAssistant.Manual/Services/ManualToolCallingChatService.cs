using System.ClientModel;
using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Tools;
using OpenAI;
using OpenAI.Chat;

namespace AIKnowledgeAssistant.Services;

// Milestones 2–3: you own the tool loop (FinishReason.ToolCalls → executor → ToolChatMessage).
public sealed class ManualToolCallingChatService
{
    private readonly ChatClient _chatClient;
    private readonly LearningToolExecutor _toolExecutor = new();
    private readonly List<ChatMessage> _messages;
    private readonly ChatCompletionOptions _options;

    public ManualToolCallingChatService(AiSettings settings, string systemPrompt)
    {
        settings.Normalize();

        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ApiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Endpoint);

        DeploymentName = settings.Model;
        ResolvedApiEndpoint = AzureEndpointHelper.NormalizeAzureEndpoint(settings.Endpoint).ToString();

        OpenAIClient openAiClient = new(
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = AzureEndpointHelper.NormalizeAzureEndpoint(settings.Endpoint)
            });

        _chatClient = openAiClient.GetChatClient(settings.Model);
        _messages = [new SystemChatMessage(systemPrompt)];

        _options = new ChatCompletionOptions();
        foreach (ChatTool tool in LearningToolDefinitions.All)
        {
            _options.Tools.Add(tool);
        }
    }

    public string DeploymentName { get; }

    public string ResolvedApiEndpoint { get; }

    public async Task<string> SendUserMessageAsync(string userText, CancellationToken cancellationToken = default)
    {
        _messages.Add(new UserChatMessage(userText));

        bool requiresAction;

        do
        {
            requiresAction = false;

            ChatCompletion completion = await _chatClient.CompleteChatAsync(_messages, _options, cancellationToken);

            switch (completion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    _messages.Add(new AssistantChatMessage(completion));
                    return completion.Content.Count > 0
                        ? completion.Content[0].Text
                        : string.Empty;

                case ChatFinishReason.ToolCalls:
                    _messages.Add(new AssistantChatMessage(completion));

                    foreach (ChatToolCall toolCall in completion.ToolCalls)
                    {
                        string toolResult = _toolExecutor.Execute(toolCall);
                        _messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                    }

                    requiresAction = true;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported finish reason: {completion.FinishReason}");
            }
        }
        while (requiresAction);

        throw new InvalidOperationException("Tool loop ended without a final assistant message.");
    }
}
