namespace API.Domain;

public sealed class RagDocument
{
    /// <summary>
    /// Internal database key for the document row.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Stable business key for the source document.
    /// </summary>
    public string DocumentKey { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable title of the source document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// File path where the source document was ingested from.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Content hash used to detect source document changes.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Flexible JSON metadata stored as jsonb for future document attributes.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// UTC timestamp indicating when the document was ingested.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp indicating when the document was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>
    /// Collection of chunks generated from this document.
    /// </summary>
    public List<RagChunk> Chunks { get; set; } = [];
}