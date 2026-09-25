namespace AIKnowledgeAssistant.Models;

// =============================================================================
// ChatMessageItem — One message in a conversation
// =============================================================================
//
// LEARNING NOTE — Chat message roles:
//   Every message in a chat completion request has a "role":
//
//   | Role       | Who wrote it   | Purpose                                      |
//   |------------|----------------|----------------------------------------------|
//   | system     | Application    | Instructions / rules for the model           |
//   | user       | Human          | The person's question or statement           |
//   | assistant  | Model          | The model's previous replies (for context)   |
//
//   Together these form the "prompt" sent to the LLM.
//   In Milestone 2 we will also see "tool" messages when the model requests tools.
//
// WHY A SIMPLE CLASS?
//   We use our own type instead of the OpenAI SDK types in Program.cs so the
//   main loop stays easy to read. The service layer converts these to SDK types.
//
// =============================================================================
public sealed class ChatMessageItem
{
    /// <summary>
    /// Message role: "system", "user", or "assistant".
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public required string Content { get; init; }
}
