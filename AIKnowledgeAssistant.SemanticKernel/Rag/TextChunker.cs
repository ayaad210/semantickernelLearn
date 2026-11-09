using System.Text;

namespace AIKnowledgeAssistant.Rag;

// RAG step 1: CHUNKING = cutting a long text into small pieces.
//
// Why? A vector for a whole file is vague, and we only want to send the useful part to the model.
// Our simple rule:
//   1. Split the text into paragraphs (blank line = new paragraph).
//   2. Put paragraphs together into one chunk until it reaches about 500 characters.
public static class TextChunker
{
    public static IReadOnlyList<DocumentChunk> Chunk(string source, string text, int maxChars = 500)
    {
        var chunks = new List<DocumentChunk>();
        var current = new StringBuilder();   // the chunk we are building now

        string[] paragraphs = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (string raw in paragraphs)
        {
            string paragraph = raw.Trim();
            if (paragraph.Length == 0)
            {
                continue;
            }

            // The current chunk would get too big: save it and start a new one.
            if (current.Length > 0 && current.Length + paragraph.Length > maxChars)
            {
                chunks.Add(new DocumentChunk(source, current.ToString().Trim()));
                current.Clear();
            }

            current.AppendLine(paragraph);
            current.AppendLine();
        }

        // Save the last chunk.
        if (current.Length > 0)
        {
            chunks.Add(new DocumentChunk(source, current.ToString().Trim()));
        }

        return chunks;
    }
}
