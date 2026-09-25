namespace AIKnowledgeAssistant.Configuration;

// =============================================================================
// AiSettings — Azure OpenAI / Microsoft Foundry (API key from appsettings.json)
// =============================================================================
//
// All three values must come from the SAME Azure resource (same page in the portal):
//   Azure Portal → your AI resource → Keys and Endpoint
//
//   - Endpoint  → copy "Endpoint" from that blade
//   - ApiKey    → copy "KEY 1" or "KEY 2" (not the Entra / project-only samples)
//   - Model     → deployment name from Model deployments (e.g. gpt-5-mini)
//
// =============================================================================
public sealed class AiSettings
{
    public const string SectionName = "Ai";

    /// <summary>Deployment name in Azure (exact name from Model deployments).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Resource endpoint from Keys and Endpoint (we append /openai/v1/ for the SDK).</summary>
    public string? Endpoint { get; set; }

    /// <summary>KEY 1 or KEY 2 from Keys and Endpoint for the same resource as Endpoint.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Trims values and supports pasting a full Azure connection string into ApiKey
    /// (Endpoint=...;Key=...) for convenience.
    /// </summary>
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
