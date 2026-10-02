using System.ClientModel;
using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Tools;
using OpenAI;
using OpenAI.Chat;

namespace AIKnowledgeAssistant.Services;

// =============================================================================
// Manual tool-calling loop (OpenAI .NET SDK / Azure Chat Completions)
// =============================================================================
//
// KEY TYPES:
//
// ChatClient — sends chat requests to Azure. Same as Milestone 1, but we pass Tools.
//
// ChatMessage — base type for conversation turns:
//   - SystemChatMessage  → instructions for the model
//   - UserChatMessage    → what the human typed
//   - AssistantChatMessage → model reply (text and/or tool call requests)
//   - ToolChatMessage    → YOUR app's tool result (role "tool" in the API)
//
// ChatCompletionOptions — extra settings for one request:
//   - Tools: collection of ChatTool definitions the model may call
//
// ChatCompletion — one response from the model:
//   - Content          → text (may be empty when only tool calls)
//   - ToolCalls        → list of ChatToolCall when model wants tools
//   - FinishReason     → why the model stopped this turn:
//       Stop       → normal text answer (done unless you want more)
//       ToolCalls  → must run tools and call API again
//
// Tool loop (official pattern):
//   1. CompleteChatAsync(messages, options)
//   2. If FinishReason == ToolCalls:
//        add AssistantChatMessage(completion)  // keeps tool call ids in history
//        run each ChatToolCall in your app
//        add ToolChatMessage(toolCallId, result) for each
//        goto 1
//   3. If FinishReason == Stop:
//        return text to user
//
// The model never executes C#. Only your LearningToolExecutor does.
// Milestone 3: write tools (CreateTask, CompleteTask) ask the human y/n inside the executor.
// =============================================================================
public sealed class ManualToolCallingChatService
{
    private readonly ChatClient _chatClient;
    private readonly LearningToolExecutor _toolExecutor = new();

    // Full conversation for the SDK (includes tool messages — not just user/assistant text).
    private readonly List<ChatMessage> _messages;

    // Reused on every API call — includes registered ChatTool definitions.
    private readonly ChatCompletionOptions _options;

    public ManualToolCallingChatService(AiSettings settings, string systemPrompt)
    {
        settings.Normalize();

        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ApiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Endpoint);

        DeploymentName = settings.Model;
        ResolvedApiEndpoint = AzureOpenAiChatService.NormalizeAzureEndpoint(settings.Endpoint).ToString();

        OpenAIClient openAiClient = new(
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = AzureOpenAiChatService.NormalizeAzureEndpoint(settings.Endpoint)
            });

        // ChatClient targets your deployment name (gpt-5-mini, etc.).
        _chatClient = openAiClient.GetChatClient(settings.Model);

        _messages = [new SystemChatMessage(systemPrompt)];

        // Register every ChatTool from LearningToolDefinitions.All (read + write).
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

            // Send history + tool definitions; get ChatCompletion back.
            ChatCompletion completion = await _chatClient.CompleteChatAsync(_messages, _options, cancellationToken);

            switch (completion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    // Model finished with a normal assistant message (text for the user).
                    _messages.Add(new AssistantChatMessage(completion));
                    return completion.Content.Count > 0
                        ? completion.Content[0].Text
                        : string.Empty;

                case ChatFinishReason.ToolCalls:
                    // Model stopped to request tool(s) — not a final answer yet.
                    // Store assistant turn WITH tool call metadata for the next request.
                    _messages.Add(new AssistantChatMessage(completion));

                    foreach (ChatToolCall toolCall in completion.ToolCalls)
                    {
                        // App runs C#; returns string result for this tool call id.
                        string toolResult = _toolExecutor.Execute(toolCall);

                        // ToolChatMessage links result to toolCall.Id (required by the API).
                        _messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                    }

                    // Call CompleteChatAsync again with updated history.
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
