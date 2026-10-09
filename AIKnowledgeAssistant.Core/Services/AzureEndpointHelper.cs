namespace AIKnowledgeAssistant.Services;

// Shared endpoint normalization for OpenAI SDK (Manual) and SK Azure connector (Semantic Kernel app).
public static class AzureEndpointHelper
{
    public static Uri NormalizeAzureEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint.Trim());
        string host = uri.Host;

        if (uri.AbsolutePath.Contains("/api/projects/", StringComparison.OrdinalIgnoreCase))
        {
            host = uri.Host;
        }

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

    // SK AddAzureOpenAIChatCompletion expects resource root, not /openai/v1/.
    public static string ToAzureResourceEndpoint(string endpoint)
    {
        Uri normalized = NormalizeAzureEndpoint(endpoint);
        return $"{normalized.Scheme}://{normalized.Host}/";
    }
}
