namespace API.Features.Rag.Ingestion;

public sealed record RagIngestionRequest(string DocumentKey, string SourcePath);