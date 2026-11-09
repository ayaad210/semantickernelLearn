# Agenda: Semantic Kernel and RAG

1. **Semantic Kernel** (6 min)
   - What it is: a Microsoft library that connects our C# code to an AI model
   - The model can't run our code, so we give it tools
   - A tool is a normal method with `[KernelFunction]` and `[Description]`
   - The loop: the model asks, our code runs, the result goes back

2. **The problem RAG solves** (2 min)
   - Models don't know our documents
   - Without them the model may guess (hallucination)
   - We can't paste every document into every question

3. **RAG** (2 min)
   - Retrieve the right text, add it to the question, let the model answer from it
   - The model is not retrained
   - The answer can name its source

4. **Vectors** (4 min)
   - A vector is a list of numbers that stands for the meaning of a text
   - Similar meaning gives similar vectors, even with different words
   - Our vectors have 1536 numbers
   - Cosine similarity finds the closest chunks, and `NOT_FOUND` stops guessing

5. **The RAG pipeline** (2 min)
   - Once at startup: documents, chunks, embeddings, vector store
   - Per question: embed the question, search, send the top 3 chunks to the model

6. **Our project** (3 min)
   - One console app with task tools, document search and a calculator
   - A multi-step example: search the leave policy, then calculate days left
   - The console shows every `[tool call]` and `[tool result]`

7. **Takeaways and questions** (1 min + Q&A)
   - Semantic Kernel connects the model to our code
   - A tool is just a method
   - RAG gives answers from facts, not guesses
   - Vectors let us search by meaning

**The story in one sentence:** the model can think but can't act or know our data, so Semantic Kernel gives it **tools**, and RAG gives it **our documents**.
