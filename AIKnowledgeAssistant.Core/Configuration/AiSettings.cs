namespace AIKnowledgeAssistant.Configuration;

// Shared Azure OpenAI settings (both console apps read the same appsettings.json shape).
public sealed class AiSettings
{
    public const string SectionName = "Ai";

    public string Model { get; set; } = "gpt-4o-mini";

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public AiSettings Normalize()
    {
        Model = Model.Trim();
        Endpoint = Endpoint?.Trim();
        ApiKey = ApiKey?.Trim();

        if (string.IsNullOrEmpty(ApiKey) ||
            !ApiKey.Contains('=', StringComparison.Ordinal) ||
            !ApiKey.Contains("Key", StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        foreach (string segment in ApiKey.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = segment.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string name = segment[..separator].Trim();
            string value = segment[(separator + 1)..].Trim();

            if (name.Equals("Endpoint", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(Endpoint))
            {
                Endpoint = value;
            }
            else if (name.Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                ApiKey = value;
            }
        }

        return this;
    }
}
