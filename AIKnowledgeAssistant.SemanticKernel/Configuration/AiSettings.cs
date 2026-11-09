namespace AIKnowledgeAssistant.Configuration;

// The settings we read from appsettings.json (the "Ai" section).
public sealed class AiSettings
{
    public const string SectionName = "Ai";

    // Name of the CHAT deployment you created in Azure (not the base model name).
    public string Model { get; set; } = "gpt-4o-mini";

    // Name of the EMBEDDING deployment (used by RAG). Same Azure resource and key as the chat model.
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    // Address of your Azure OpenAI resource.
    public string? Endpoint { get; set; }

    // Secret key. Keep it only in appsettings.json, which git ignores.
    public string? ApiKey { get; set; }
}
