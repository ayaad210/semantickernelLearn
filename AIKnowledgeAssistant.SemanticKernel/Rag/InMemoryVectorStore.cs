namespace AIKnowledgeAssistant.Rag;

// RAG step 3 - VECTOR STORE (hand-written, no database).
// It is just a list of (chunk, vector) pairs. "Search" = compare the question vector with every
// stored vector and return the closest ones. Fine for a handful of chunks; real systems use an index.
public sealed class InMemoryVectorStore
{
    private readonly List<(DocumentChunk Chunk, ReadOnlyMemory<float> Vector)> _items = new();

    public int Count => _items.Count;

    public void Add(DocumentChunk chunk, ReadOnlyMemory<float> vector) => _items.Add((chunk, vector));

    // RAG step 5 - SIMILARITY SEARCH: score every item, keep the best topK above minScore.
    public IReadOnlyList<(DocumentChunk Chunk, double Score)> Search(
        ReadOnlyMemory<float> queryVector, int topK = 3, double minScore = 0.3)
    {
        var results = new List<(DocumentChunk Chunk, double Score)>();
        foreach (var item in _items)
        {
            double score = CosineSimilarity(queryVector.Span, item.Vector.Span);
            if (score >= minScore)
            {
                results.Add((item.Chunk, score));
            }
        }

        return results.OrderByDescending(r => r.Score).Take(topK).ToList();
    }

    // Cosine similarity = how much two vectors point in the same direction.
    //   1.0 = same meaning, ~0 = unrelated. Formula: (a . b) / (|a| * |b|)
    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
