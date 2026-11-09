# Semantic Kernel and RAG

## Slide 1: Title

**Semantic Kernel and RAG**
Making an AI model use our code and our documents

| Image |
| --- |
| A simple robot or chat bubble next to a document icon and a gear icon. Any clean icon set works. |

---

## Slide 2: What is Semantic Kernel?

- A free, open-source library from Microsoft for building AI apps in C#, Python and Java.
- It connects our code to an AI model (Azure OpenAI, OpenAI and others).
- It handles the boring parts: tools, chat history, and calling the model again and again.
- Think of it as the **glue** between the model and our application.

| Image |
| --- |
| The Semantic Kernel logo, or a diagram with 3 boxes: **Our app** - **Semantic Kernel** - **AI model**. |

---

## Slide 3: The model cannot run our code

- A model only reads text and writes text.
- It cannot read our database, call our API or know our company rules.
- So we give it **tools**.
- A tool is a normal C# method. The model **asks**, our code **runs**.

| Image |
| --- |
| A person (the model) behind glass handing a note that says "please call ListTasks" to a worker (our code). The worker returns the result. |

---

## Slide 4: How a tool works in Semantic Kernel

- We write a normal method and add two attributes:
  - `[KernelFunction]` means "this is a tool".
  - `[Description]` is what the model reads to know when to use it.
- Semantic Kernel builds the tool description for the model automatically.
- A group of tools in one class is called a **plugin**.

| Image |
| --- |
| A screenshot of `TaskManagementPlugin.cs` showing the two attributes above a method. |

---

## Slide 5: The tool loop

1. User asks a question.
2. Model replies: "call this tool with these arguments".
3. Our code runs the tool.
4. The result goes back to the model.
5. The model answers (or asks for another tool).

Semantic Kernel repeats steps 2 to 5 for us, with one line: `FunctionChoiceBehavior.Auto()`.

| Image |
| --- |
| A circular arrow diagram: **Model** to **Tool call** to **Our C# code** to **Result** back to **Model**. |

---

## Slide 6: The problem: the model does not know our documents

- Models are trained on public data. They do not know our leave policy or internal notes.
- If we ask anyway, the model may **guess** with a confident answer that is wrong. This is called a **hallucination**.
- We cannot paste every document into every question: it is slow, costly and too long.

| Image |
| --- |
| A chat bubble with a confident but wrong answer, with a red warning mark. |

---

## Slide 7: What is RAG?

**R**etrieval **A**ugmented **G**eneration

1. **Retrieve**: find the few pieces of text that are relevant to the question.
2. **Augment**: give those pieces to the model together with the question.
3. **Generate**: the model answers **from that text**, and can name the source.

The model is not retrained. We just show it the right pages at the right time.

| Image |
| --- |
| Open-book exam: a student looking at a book while answering. Caption: "RAG = an open-book exam for the model". |

---

## Slide 8: What is a vector?

- A **vector** is a list of numbers.
- An **embedding model** turns a piece of text into a vector that stands for its **meaning**.
- Texts with similar meaning get similar vectors, even if the words are different.
  - "How many vacation days do I get?"
  - "Annual leave entitlement"
  - These two have very close vectors.

| Image |
| --- |
| A sentence on the left, an arrow, and a row of numbers on the right (see the next slide). |

---

## Slide 9: What a vector looks like

```
"annual leave days"  ->  [ 0.021, -0.134, 0.457, 0.008, ... ]
```

- In our project the vector has **1536 numbers** (model: `text-embedding-3-small`).
- Each number alone means nothing to a human. Only the **whole list** carries the meaning.
- Easy picture: imagine a map with only 2 numbers (x, y). Real vectors are the same idea, but with 1536 directions instead of 2.

| Image |
| --- |
| A 2D scatter plot ("meaning map"). Points for "leave policy" and "vacation days" are close together. Points for "server deployment" are far away in another corner. |

---

## Slide 10: How do we compare vectors?

- To find the best match we compare the **question vector** with every **chunk vector**.
- We use **cosine similarity**: do the two vectors point in the same direction?
  - Close to **1**: same meaning.
  - Close to **0**: unrelated.
- We keep the top 3 results above a minimum score (0.3).
- If nothing is close enough, we return `NOT_FOUND`, and the model says "not found" instead of guessing.

| Image |
| --- |
| Two arrows from the same point. One pair with a small angle, labelled "similar". One pair with a wide angle, labelled "different". |

---

## Slide 11: The RAG pipeline

**Once, at startup (indexing)**
Documents, then **Chunks** (small pieces), then **Embeddings** (vectors), then a **Vector store**

**For every question (search)**
Question, then **Embedding**, then **Similarity search**, then **Top 3 chunks**, then **Model answers from them**

| Image |
| --- |
| A two-row flow chart. Top row: Documents - Chunks - Embeddings - Vector store. Bottom row: Question - Embedding - Search - Top chunks - Answer. |

---

## Slide 12: Our project (one slide)

**A console app: Semantic Kernel + RAG, in one small project**

- **Task tools**: `ListTasks`, `CreateTask`, `CompleteTask` (a fake in-memory list).
- **Document tool**: `SearchDocuments` searches 3 text files (leave policy, company rules, technical notes) with RAG.
- **Calculator tool**: `CalculateRemainingDays`, used after a search in multi-step questions.
- The model chooses the tool. Our C# runs it. The console prints each `[tool call]` and `[tool result]`.
- Example: *"I used 5 leave days, how many remain?"* gives search, then calculate, then **15 days** with the source file.

| Image |
| --- |
| A screenshot of the console showing the `[tool call]` and `[tool result]` lines and the final answer for the leave question. Take it after running the app. |

---

## Slide 13: Key takeaways

- Semantic Kernel connects the model to our C# code.
- A tool is just a method: the model asks, our code runs.
- RAG gives the model our documents, so it answers from facts, not guesses.
- Vectors turn meaning into numbers, so we can search by meaning, not only by words.
- Always show the source and handle "not found".

| Image |
| --- |
| A simple check-mark list, or the logo of Semantic Kernel again. |

---

## Notes for the speaker

- Slides 2 to 5 are about Semantic Kernel, slides 6 to 11 are about RAG and vectors, slide 12 is the project.
- If you only have 10 minutes, skip slide 10 and say the cosine idea in one sentence.
- Run the app once before the talk and take the screenshot for slide 12. Use the question from the slide so the output matches.
- Possible question: "Does the model learn our documents?" Answer: no. We only send it the relevant text for each question.
