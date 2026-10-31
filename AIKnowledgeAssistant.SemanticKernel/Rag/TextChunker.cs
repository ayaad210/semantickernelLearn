using System.Text;

namespace AIKnowledgeAssistant.Rag;

// RAG step 1 - CHUNKING.
// We never embed a whole file: a vector for a long text becomes vague, and we only want to send the
// relevant part to the model. Strategy here is deliberately simple:
//   split on blank lines (paragraphs/sections), then merge small paragraphs until ~maxChars.
public static class TextChunker
{
    public static IReadOnlyList<DocumentChunk> Chunk(string source, string text, int maxChars = 500)
    {
        var chunks = new List<DocumentChunk>();
        var current = new StringBuilder();

        string[] paragraphs = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (string raw in paragraphs)
        {
            string paragraph = raw.Trim();
            if (paragraph.Length == 0)
            {
                continue;
            }

            // Current chunk is full: save it and start a new one.
            if (current.Length > 0 && current.Length + paragraph.Length > maxChars)
            {
                chunks.Add(new DocumentChunk(source, current.ToString().Trim()));
                current.Clear();
            }

            current.AppendLine(paragraph);
            current.AppendLine();
        }

        if (current.Length > 0)
        {
            chunks.Add(new DocumentChunk(source, current.ToString().Trim()));
        }

        return chunks;
    }
}
