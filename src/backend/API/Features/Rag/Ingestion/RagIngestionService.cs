using API.Domain;
using API.Features.Rag.Shared;
using API.Infrastructure.Options;
using API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using System.Security.Cryptography;
using System.Text;

namespace API.Features.Rag.Ingestion;

public sealed class RagIngestionService
{
    private readonly MemDbContext _dbContext;
    private readonly RagEmbeddingService _embeddingService;
    private readonly RagOptions _options;

    public RagIngestionService(MemDbContext dbContext, RagEmbeddingService embeddingService, IOptions<RagOptions> options)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _options = options.Value;
    }

    public async Task<RagIngestionResult> RebuildDocumentAsync(RagIngestionRequest request, CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(request.SourcePath);
        var pages = RagPdfReader.ReadPages(sourcePath);
        var sourceHash = await ComputeHashAsync(sourcePath, cancellationToken);

        var existingDocument = await _dbContext.Documents
            .Include(x => x.Chunks)
            .SingleOrDefaultAsync(x => x.DocumentKey == request.DocumentKey, cancellationToken);

        if (existingDocument is not null)
        {
            _dbContext.Documents.Remove(existingDocument);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var document = new RagDocument
        {
            DocumentKey = request.DocumentKey,
            Title = Path.GetFileNameWithoutExtension(sourcePath),
            SourcePath = sourcePath,
            ContentHash = sourceHash,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var chunkIndex = 0;

        foreach (var page in pages)
        {
            foreach (var chunkText in RagTextChunker.Chunk(page.Text, _options.ChunkSize, _options.ChunkOverlap))
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken);

                document.Chunks.Add(new RagChunk
                {
                    RagDocumentId = document.Id,
                    ChunkIndex = chunkIndex++,
                    PageNumber = page.PageNumber,
                    SectionTitle = $"Page {page.PageNumber}",
                    ChunkText = chunkText,
                    SourceExcerpt = TrimExcerpt(chunkText),
                    Embedding = new Vector(embedding),
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        document.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RagIngestionResult(request.DocumentKey, chunkIndex, sourcePath);
    }

    private static string TrimExcerpt(string text, int maxLength = 220)
        => text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "...";

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}