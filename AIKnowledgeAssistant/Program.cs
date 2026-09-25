// =============================================================================
// Program.cs — Milestone 1: Basic Chat
// =============================================================================
//
// WHAT THIS FILE DOES:
//   This is the entry point of the console application. It runs a simple chat
//   loop: read user input → send messages to the LLM → print the reply.
//
// KEY AI CONCEPTS IN THIS MILESTONE:
//   - LLM (Large Language Model): A model that generates text based on input.
//   - Prompt: Everything we send to the model (system + user + assistant messages).
//   - System message: Standing instructions that shape how the model behaves.
//   - User message: What the human types.
//   - Assistant message: What the model returns.
//   - Chat history: Previous messages sent again so the model has context.
//
// KUBERNETES ANALOGY:
//   At this stage the model is like an assistant that can only TALK about tasks.
//   It cannot run kubectl, list pods, or touch your cluster. Execution comes
//   later when we add tools (Milestone 2+). The model proposes; the app executes.
//
// =============================================================================

using AIKnowledgeAssistant.Configuration;
using AIKnowledgeAssistant.Models;
using AIKnowledgeAssistant.Services;
using Microsoft.Extensions.Configuration;

// -----------------------------------------------------------------------------
// SYSTEM MESSAGE (part of the "prompt")
// -----------------------------------------------------------------------------
// The system message is sent on every API call. It tells the model how to behave.
// It is NOT shown to the end user in the console, but the model always sees it.
// Think of it as permanent instructions for the assistant's personality and rules.
const string SystemPrompt =
    """
    You are a helpful learning assistant for a .NET AI project.
    Answer clearly and concisely.
    In later milestones, this app will add tools, but for now you only chat with text.
    """;

// -----------------------------------------------------------------------------
// CONFIGURATION
// -----------------------------------------------------------------------------
// We load settings from:
//   1. appsettings.json  → endpoint, deployment name, API key (simple for local learning)
//   2. Environment variables → optional override, e.g. Ai__ApiKey
//
// LEARNING NOTE: In production you normally avoid storing keys in files that might be committed.
// For this learning project you asked to keep the key in appsettings.json on your machine.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Bind the "Ai" section from config into our strongly-typed AiSettings class.
var aiSettings = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>()
    ?? throw new InvalidOperationException($"Missing '{AiSettings.SectionName}' configuration section.");

aiSettings.Normalize();

// Create the Azure OpenAI chat service.
// Program.cs depends on IChatService so the main loop stays free of SDK/HTTP details.
if (string.IsNullOrWhiteSpace(aiSettings.ApiKey))
{
    throw new InvalidOperationException(
        "Missing API key. Add \"ApiKey\" under the \"Ai\" section in appsettings.json.");
}

IChatService chatService = new AzureOpenAiChatService(aiSettings);

// -----------------------------------------------------------------------------
// CHAT HISTORY
// -----------------------------------------------------------------------------
// Chat history is a list of messages we send to the model on EVERY request.
// The model is stateless: it does not remember previous turns by itself.
// We must resend the full conversation each time if we want context.
//
// We start with one system message. User and assistant messages are added
// inside the loop as the conversation continues.
var chatHistory = new List<ChatMessageItem>
{
    new() { Role = "system", Content = SystemPrompt }
};

Console.WriteLine("AI Tool-Using Knowledge Assistant");
Console.WriteLine("Milestone 1: Basic Chat");
Console.WriteLine("Provider: Azure OpenAI");
Console.WriteLine($"Deployment: {chatService.DeploymentName}");
Console.WriteLine($"API base URL: {chatService.ResolvedApiEndpoint}");
Console.WriteLine("Type your message and press Enter. Type 'exit' to quit.");
Console.WriteLine();

// -----------------------------------------------------------------------------
// CHAT LOOP (REPL — Read-Eval-Print Loop)
// -----------------------------------------------------------------------------
while (true)
{
    Console.Write("You: ");
    string? userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    // Step 1: Add the user's message to history (role = "user").
    chatHistory.Add(new ChatMessageItem { Role = "user", Content = userInput });

    try
    {
        // Step 2: Send the ENTIRE history to the model and wait for text back.
        // The service calls your Azure OpenAI deployment over HTTPS.
        // The model only returns text — it does not run any of our C# code.
        string assistantReply = await chatService.CompleteChatAsync(chatHistory);

        // Step 3: Store the assistant reply in history for the next turn.
        chatHistory.Add(new ChatMessageItem { Role = "assistant", Content = assistantReply });

        Console.WriteLine();
        Console.WriteLine($"Assistant: {assistantReply}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        // If the API call fails, remove the failed user message from history
        // so the next retry does not send a "dangling" user turn without a reply.
        Console.WriteLine();
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.Message.Contains("DeploymentNotFound", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine("Hint: Ai:Model must match a deployment on THIS resource (exact name from the portal).");
            Console.WriteLine($"      This app sent deployment name: {chatService.DeploymentName}");
            Console.WriteLine("      Portal → your resource → Model deployments / Deployments → copy Deployment name.");
        }

        if (ex.Message.Contains("401", StringComparison.Ordinal) ||
            ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine("Hint (HTTP 401): Endpoint and ApiKey must be a matching pair from the SAME resource.");
            Console.WriteLine("      Portal → rofidamohammad984-0122-resource → Keys and Endpoint");
            Console.WriteLine("      Copy Endpoint + KEY 1 into appsettings.json (not a key from semantikerneltest).");
            Console.WriteLine($"      API base URL used: {chatService.ResolvedApiEndpoint}");
        }

        Console.WriteLine("The last user message was removed from chat history.");
        chatHistory.RemoveAt(chatHistory.Count - 1);
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye.");
