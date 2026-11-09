namespace AIKnowledgeAssistant.Services;

public static class AzureEndpointHelper
{
    // Semantic Kernel wants only the base address: https://<name>.openai.azure.com/
    // People often paste a longer address from the Azure portal, so we keep just the host.
    // Addresses ending in .services.ai.azure.com (Azure AI Foundry) are mapped to .openai.azure.com.
    public static string ToResourceEndpoint(string endpoint)
    {
        string host = new Uri(endpoint.Trim()).Host;

        const string foundrySuffix = ".services.ai.azure.com";
        if (host.EndsWith(foundrySuffix, StringComparison.OrdinalIgnoreCase))
        {
            host = host[..^foundrySuffix.Length] + ".openai.azure.com";
        }

        return $"https://{host}/";
    }
}
