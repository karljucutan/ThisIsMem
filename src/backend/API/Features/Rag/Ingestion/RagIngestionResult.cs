namespace API.Features.Rag.Ingestion;

public sealed record RagIngestionResult(string DocumentKey, int ChunkCount, string SourcePath);