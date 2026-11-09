using System.ComponentModel;
using AIKnowledgeAssistant.Rag;
using Microsoft.SemanticKernel;

namespace AIKnowledgeAssistant.Plugins;

// RAG is exposed to the model as a normal tool, just like the task tools.
// The model decides WHEN to search. Our C# code does the searching.
public sealed class DocumentPlugin(DocumentSearchService search)
{
    [KernelFunction("SearchDocuments")]
    [Description("Semantic search over local company documents (leave policy, company rules, technical notes). " +
                 "Returns relevant text with its source file, or NOT_FOUND.")]
    public Task<string> SearchDocuments([Description("What to look for, in natural language")] string query) =>
        search.SearchAsync(query);
}
