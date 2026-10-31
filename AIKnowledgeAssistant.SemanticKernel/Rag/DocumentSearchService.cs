using System.Text;
using Microsoft.Extensions.AI;

namespace AIKnowledgeAssistant.Rag;

// Orchestrates the RAG pipeline:
//   INDEXING (once at startup): files -> chunks -> embeddings -> vector store
//   SEARCHING (per question):   question -> embedding -> similarity search -> text for the model
public sealed class DocumentSearchService
{
    // Microsoft.Extensions.AI abstraction over the Azure embedding deployment (text-embedding-3-small).
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly InMemoryVectorStore _store = new();

    public DocumentSearchService(IEmbeddingGenerator<string, Embedding<float>> embeddings)
    {
        _embeddings = embeddings;
    }

    public int ChunkCount => _store.Count;

    // RAG steps 1-3: read each .txt file, chunk it, embed each chunk (calls Azure), store the vector.
    public async Task IndexFolderAsync(string folder, CancellationToken cancellationToken = default)
    {
        foreach (string file in Directory.GetFiles(folder, "*.txt"))
        {
            string text = await File.ReadAllTextAsync(file, cancellationToken);

            foreach (DocumentChunk chunk in TextChunker.Chunk(Path.GetFileName(file), text))
            {
                // RAG step 2 - EMBEDDING: text -> vector (1536 numbers for text-embedding-3-small).
                ReadOnlyMemory<float> vector = await _embeddings.GenerateVectorAsync(
                    chunk.Text, cancellationToken: cancellationToken);
                _store.Add(chunk, vector);
            }
        }
    }

    // RAG steps 4-6: the question is embedded with the SAME model, compared with all chunks,
    // and the best matches are returned as plain text. This string becomes the TOOL RESULT the model reads.
    public async Task<string> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<float> queryVector = await _embeddings.GenerateVectorAsync(
            query, cancellationToken: cancellationToken);

        var results = _store.Search(queryVector);

        // No chunk was similar enough: tell the model explicitly so it can answer "not found" instead of guessing.
        if (results.Count == 0)
        {
            return "NOT_FOUND: no relevant text in the local documents.";
        }

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
