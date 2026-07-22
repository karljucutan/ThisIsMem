namespace API.Features.Rag.Inference;

public sealed class RagSemanticSearchResult
{
    public string DocumentKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public int? PageNumber { get; set; }

    public string SectionTitle { get; set; } = string.Empty;

    public string ChunkText { get; set; } = string.Empty;

    public double Distance { get; set; }
}