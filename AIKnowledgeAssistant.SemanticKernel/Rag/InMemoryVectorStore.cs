namespace AIKnowledgeAssistant.Rag;

// RAG step 3: the VECTOR STORE. We wrote it by hand, so there is no database.
//
// A vector is a list of numbers that represents the MEANING of a text.
// Texts with similar meaning have similar vectors.
// The store is just a list of (chunk, vector) pairs kept in memory.
public sealed class InMemoryVectorStore
{
    private readonly List<(DocumentChunk Chunk, ReadOnlyMemory<float> Vector)> _items = new();

    public int Count => _items.Count;

    public void Add(DocumentChunk chunk, ReadOnlyMemory<float> vector) => _items.Add((chunk, vector));

    // RAG step 4: SIMILARITY SEARCH.
    // Compare the question's vector with every stored vector, then keep the best matches:
    //   - topK = 3      -> at most 3 results
    //   - minScore = 0.3 -> ignore results that are not similar enough
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

        // Best score first.
        return results.OrderByDescending(r => r.Score).Take(topK).ToList();
    }

    // Cosine similarity = "do these two vectors point the same way?"
    //   close to 1 = same meaning, close to 0 = unrelated.
    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];       // how much they agree
            normA += a[i] * a[i];     // length of a
            normB += b[i] * b[i];     // length of b
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
