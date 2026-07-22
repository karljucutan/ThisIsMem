using Pgvector;

namespace API.Domain;

public sealed class RagChunk
{
    /// <summary>
    /// Internal database key for the chunk row.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to the parent RAG document.
    /// </summary>
    public long RagDocumentId { get; set; }

    /// <summary>
    /// Chunk order within the source document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Source PDF page number when available.
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Human-readable section or page label for traceability.
    /// </summary>
    public string SectionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Chunk text used for embedding generation and retrieval.
    /// </summary>
    public string ChunkText { get; set; } = string.Empty;

    /// <summary>
    /// Short excerpt returned with search results for quick inspection.
    /// </summary>
    public string SourceExcerpt { get; set; } = string.Empty;

    /// <summary>
    /// Vector embedding stored in pgvector for semantic similarity search.
    /// </summary>
    public Vector Embedding { get; set; } = default!;

    /// <summary>
    /// UTC timestamp indicating when the chunk was ingested.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}