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
using System.Text.RegularExpressions;

namespace API.Features.Rag.Ingestion;

public sealed partial class RagIngestionService
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
        var sourcePath = request.SourcePath;
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
            ingestionSourcePath = await PersistCanonicalMarkdownAsync(sourcePath, cancellationToken);
        }

        // MarkdownReader will not work correctly for RAG ingestion because it strips out HTML elements (e.g. tables) that are produced by Azure Document Intelligence. Instead, we use a custom reader that preserves all content verbatim.
        // IngestionDocumentReader reader = new MarkdownReader();
        IngestionDocumentReader reader = new RagMarkdownReader();
        var tokenizer = TiktokenTokenizer.CreateForModel(_options.ChunkingTokenizerModel);
        var chunkerOptions = new IngestionChunkerOptions(tokenizer)
        {
            MaxTokensPerChunk = _options.ChunkSize,
            OverlapTokens = _options.ChunkOverlap,
        };

        var chunker = new HeaderChunker(chunkerOptions);
        var pageSections = await SplitMarkdownIntoPageFilesAsync(ingestionSourcePath, cancellationToken);

        if (pageSections.Count == 0)
        {
            await ProcessFileAsync(reader, chunker, document, ingestionSourcePath, pageNumber: null, cancellationToken);
        }
        else
        {
            try
            {
                foreach (var pageSection in pageSections)
                {
                    await ProcessFileAsync(reader, chunker, document, pageSection.FilePath, pageSection.PageNumber, cancellationToken);
                }
            }
            finally
            {
                DeletePageSectionFiles(pageSections);
            }
        }

        document.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RagIngestionResult(request.DocumentKey, document.Chunks.Count, sourcePath);
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private async Task<string> PersistCanonicalMarkdownAsync(
        string sourcePdfPath,
        CancellationToken cancellationToken)
    {
        var markdownPath = Path.ChangeExtension(sourcePdfPath, ".md");
        var (_, fullMarkdownContent) = await _pdfReader.ReadPagesAsync(sourcePdfPath, cancellationToken);
        await File.WriteAllTextAsync(markdownPath, fullMarkdownContent, Encoding.UTF8, cancellationToken);
        return markdownPath;
    }

    private async Task ProcessFileAsync(
        IngestionDocumentReader reader,
        IngestionChunker<string> chunker,
        RagDocument document,
        string filePath,
        int? pageNumber,
        CancellationToken cancellationToken)
    {
        var writer = new RagIngestionChunkWriter(document, _embeddingService, pageNumber);
        using var pipeline = new IngestionPipeline<string>(reader, chunker, writer);

        await foreach (var result in pipeline.ProcessAsync([new FileInfo(filePath)], cancellationToken))
        {
            if (!result.Succeeded)
                throw new InvalidOperationException($"RAG ingestion failed for '{result.DocumentId}'.", result.Exception);
        }
    }

    private static async Task<List<PageSectionFile>> SplitMarkdownIntoPageFilesAsync(
        string markdownPath,
        CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(markdownPath).Equals(".md", StringComparison.OrdinalIgnoreCase))
            return [];

        var markdown = await File.ReadAllTextAsync(markdownPath, cancellationToken);
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var pageFiles = new List<PageSectionFile>();
        var pageContent = new StringBuilder();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"thisismem-rag-pages-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                var match = PageNumberCommentRegex().Match(line);

                if (match.Success)
                {
                    if (int.TryParse(match.Groups["page"].Value, out var pageNumber))
                    {
                        await WritePageSectionAsync(pageFiles, tempDirectory, pageNumber, pageContent, cancellationToken);
                        pageContent.Clear();
                    }

                    continue;
                }

                if (line.Contains("<!-- PageFooter=", StringComparison.Ordinal) ||
                    line.Contains("<!-- PageBreak -->", StringComparison.Ordinal))
                {
                    continue;
                }

                pageContent.AppendLine(rawLine);
            }

            if (pageFiles.Count > 0)
            {
                var lastPageNumber = pageFiles[^1].PageNumber + 1;
                await WritePageSectionAsync(pageFiles, tempDirectory, lastPageNumber, pageContent, cancellationToken);
            }

            if (pageFiles.Count == 0)
            {
                Directory.Delete(tempDirectory, recursive: true);
                return [];
            }

            return pageFiles;
        }
        catch
        {
            Directory.Delete(tempDirectory, recursive: true);
            throw;
        }
    }

    private static async Task WritePageSectionAsync(
        ICollection<PageSectionFile> pageFiles,
        string tempDirectory,
        int pageNumber,
        StringBuilder pageContent,
        CancellationToken cancellationToken)
    {
        var content = pageContent.ToString().Trim();
        if (string.IsNullOrWhiteSpace(content))
            return;

        var filePath = Path.Combine(tempDirectory, $"page-{pageNumber:D4}.md");
        await File.WriteAllTextAsync(filePath, content + Environment.NewLine, Encoding.UTF8, cancellationToken);
        pageFiles.Add(new PageSectionFile(pageNumber, filePath));
    }

    private static void DeletePageSectionFiles(IEnumerable<PageSectionFile> pageSections)
    {
        var directory = pageSections
            .Select(x => Path.GetDirectoryName(x.FilePath))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (directory is not null && Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private sealed record PageSectionFile(int PageNumber, string FilePath);

    [GeneratedRegex(@"^\s*<!--\s*PageNumber=""(?<page>\d+)""\s*-->\s*$", RegexOptions.Compiled)]
    private static partial Regex PageNumberCommentRegex();
}