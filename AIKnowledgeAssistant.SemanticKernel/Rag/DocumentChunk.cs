namespace AIKnowledgeAssistant.Rag;

// One searchable piece of a document. Source = file name, so answers can cite where the text came from.
public sealed record DocumentChunk(string Source, string Text);
