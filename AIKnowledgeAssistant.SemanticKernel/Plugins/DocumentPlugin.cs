using System.ComponentModel;
using AIKnowledgeAssistant.Rag;
using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Plugins;

// RAG exposed as a normal tool. The model decides WHEN to search; our C# does the search.
// Read-only: it never changes data, so SkToolVisibilityFilter logs it but does not ask for y/n.
public sealed class DocumentPlugin
{
    private readonly DocumentSearchService _search;

    public DocumentPlugin(DocumentSearchService search)
    {
        _search = search;
    }

    [KernelFunction("SearchDocuments")]
    [Description("Semantic search over local company documents (leave policy, company rules, technical notes). " +
                 "Returns relevant text with its source file, or NOT_FOUND. Read-only.")]
    public Task<string> SearchDocuments([Description("What to look for, in natural language")] string query) =>
        _search.SearchAsync(query);
}
