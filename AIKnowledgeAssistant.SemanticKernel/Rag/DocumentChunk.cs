namespace AIKnowledgeAssistant.Rag;

// One small piece of a document.
// We keep the file name (Source) so the answer can say where the text came from.
public sealed record DocumentChunk(string Source, string Text);
