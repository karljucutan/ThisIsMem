using API.Domain;
using API.Features.Rag.Shared;
using API.Infrastructure.Options;
using API.Infrastructure.Persistence;
using Microsoft.Extensions.DataIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.ML.Tokenizers;
using System.Security.Cryptography;

namespace API.Features.Rag.Ingestion;

public sealed class RagIngestionService
{
    private readonly MemDbContext _dbContext;
    private readonly RagEmbeddingService _embeddingService;
    private readonly RagPdfReader _pdfReader;
    private readonly RagOptions _options;

    public RagIngestionService(MemDbContext dbContext, RagEmbeddingService embeddingService, RagPdfReader pdfReader, IOptions<RagOptions> options)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _pdfReader = pdfReader;
        _options = options.Value;
    }

    public async Task<RagIngestionResult> RebuildDocumentAsync(RagIngestionRequest request, CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(request.SourcePath);
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

        // Consider persisting the extracted document as a canonical Markdown artifact,
        // along with the raw Azure Document Intelligence result, so re-chunking and
        // re-indexing can be done without repeating text extraction.
        var reader = new RagIngestionDocumentReader(_pdfReader);
        var tokenizer = TiktokenTokenizer.CreateForModel(_options.ChunkingTokenizerModel);
        var chunkerOptions = new IngestionChunkerOptions(tokenizer)
        {
            MaxTokensPerChunk = _options.ChunkSize,
            OverlapTokens = _options.ChunkOverlap,
        };

        var chunker = new HeaderChunker(chunkerOptions);
        var writer = new RagIngestionChunkWriter(document, _embeddingService);

        using var pipeline = new IngestionPipeline<string>(reader, chunker, writer);

        await foreach (var result in pipeline.ProcessAsync([new FileInfo(sourcePath)], cancellationToken))
        {
            if (!result.Succeeded)
                throw new InvalidOperationException($"RAG ingestion failed for '{result.DocumentId}'.", result.Exception);
        }

        document.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RagIngestionResult(request.DocumentKey, writer.ChunkCount, sourcePath);
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}