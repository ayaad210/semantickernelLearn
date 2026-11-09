using System.Text;
using Microsoft.Extensions.AI;

namespace AIKnowledgeAssistant.Rag;

// The RAG pipeline in one class. It has two jobs:
//
//   INDEXING (once, at startup):  files -> chunks -> embeddings -> vector store
//   SEARCHING (every question):   question -> embedding -> find similar chunks -> text for the model
public sealed class DocumentSearchService
{
    // Turns text into a vector (a list of numbers) using the Azure embedding model.
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;

    // Where the chunks and their vectors are kept.
    private readonly InMemoryVectorStore _store = new();

    public DocumentSearchService(IEmbeddingGenerator<string, Embedding<float>> embeddings)
    {
        _embeddings = embeddings;
    }

    public int ChunkCount => _store.Count;

    // INDEXING: for every .txt file, cut it into chunks, embed each chunk, and store it.
    public async Task IndexFolderAsync(string folder, CancellationToken cancellationToken = default)
    {
        foreach (string file in Directory.GetFiles(folder, "*.txt"))
        {
            string text = await File.ReadAllTextAsync(file, cancellationToken);

            foreach (DocumentChunk chunk in TextChunker.Chunk(Path.GetFileName(file), text))
            {
                // RAG step 2: EMBEDDING = text -> vector (1536 numbers for text-embedding-3-small).
                // This is a call to Azure.
                ReadOnlyMemory<float> vector = await _embeddings.GenerateVectorAsync(
                    chunk.Text, cancellationToken: cancellationToken);
                _store.Add(chunk, vector);
            }
        }
    }

    // SEARCHING: find the chunks closest in meaning to the question.
    // The returned text becomes the TOOL RESULT that the model reads before it answers.
    public async Task<string> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        // Embed the question with the SAME model, so the vectors can be compared.
        ReadOnlyMemory<float> queryVector = await _embeddings.GenerateVectorAsync(
            query, cancellationToken: cancellationToken);

        var results = _store.Search(queryVector);

        // Nothing similar enough: say so clearly, so the model answers "not found" instead of guessing.
        if (results.Count == 0)
        {
            return "NOT_FOUND: no relevant text in the local documents.";
        }

        // Put each match in the text, with its source file and score.
        var sb = new StringBuilder();
        foreach (var (chunk, score) in results)
        {
            sb.AppendLine($"[source: {chunk.Source} | similarity: {score:F2}]");
            sb.AppendLine(chunk.Text);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
