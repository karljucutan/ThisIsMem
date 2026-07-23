using API.Domain;
using API.Features.Rag.Shared;
using API.Infrastructure.Options;
using API.Infrastructure.Persistence;
using Microsoft.Extensions.DataIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.ML.Tokenizers;
using System.Security.Cryptography;
using System.Text;

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
        var ingestionSourcePath = sourcePath;

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

        if (Path.GetExtension(sourcePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pages = await _pdfReader.ReadPagesAsync(sourcePath, cancellationToken);
            ingestionSourcePath = await PersistCanonicalMarkdownAsync(sourcePath, pages, cancellationToken);
        }

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

        await foreach (var result in pipeline.ProcessAsync([new FileInfo(ingestionSourcePath)], cancellationToken))
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

    private static async Task<string> PersistCanonicalMarkdownAsync(
        string sourcePdfPath,
        IReadOnlyList<RagPdfPage> pages,
        CancellationToken cancellationToken)
    {
        var markdownPath = Path.ChangeExtension(sourcePdfPath, ".md");
        var markdown = BuildCanonicalMarkdown(pages);
        await File.WriteAllTextAsync(markdownPath, markdown, Encoding.UTF8, cancellationToken);
        return markdownPath;
    }

    private static string BuildCanonicalMarkdown(IReadOnlyList<RagPdfPage> pages)
    {
        var builder = new StringBuilder();

        foreach (var page in pages)
        {
            if (builder.Length > 0)
                builder.AppendLine();

            builder.AppendLine($"## Page {page.PageNumber}");
            builder.AppendLine();
            builder.AppendLine(string.IsNullOrWhiteSpace(page.Text) ? "(empty page)" : page.Text.Trim());
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }
}