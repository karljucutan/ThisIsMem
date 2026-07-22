namespace API.Infrastructure.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";

    public int ChunkSize { get; set; } = 1200;

    public int ChunkOverlap { get; set; } = 200;

    public int EmbeddingDimensions { get; set; } = 1536;
}