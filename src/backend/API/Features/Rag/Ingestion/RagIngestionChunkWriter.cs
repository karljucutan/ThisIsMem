using API.Domain;
using API.Features.Rag.Shared;
using Microsoft.Extensions.DataIngestion;
using Pgvector;
using System.Globalization;

namespace API.Features.Rag.Ingestion;

public sealed class RagIngestionChunkWriter : IngestionChunkWriter<string>
{
    private readonly RagDocument _document;
    private readonly RagEmbeddingService _embeddingService;
    private readonly int? _defaultPageNumber;
    private int _chunkIndex;

    public RagIngestionChunkWriter(
        RagDocument document,
        RagEmbeddingService embeddingService,
        int? defaultPageNumber = null)
    {
        _document = document;
        _embeddingService = embeddingService;
        _defaultPageNumber = defaultPageNumber;
        _chunkIndex = document.Chunks.Count;
    }

    public int ChunkCount => _chunkIndex;

    public override async Task WriteAsync(
        IAsyncEnumerable<IngestionChunk<string>> chunks,
        CancellationToken cancellationToken)
    {
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            var chunkText = chunk.Content.Trim();

            if (string.IsNullOrWhiteSpace(chunkText))
                continue;

            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkText, cancellationToken);
            var pageNumber = ResolveChunkPageNumber(chunk);
            var sectionTitle = chunk.Context;
            if (string.IsNullOrWhiteSpace(sectionTitle))
                sectionTitle = pageNumber.HasValue ? $"Page {pageNumber.Value}" : "Document";

            _document.Chunks.Add(new RagChunk
            {
                RagDocumentId = _document.Id,
                ChunkIndex = _chunkIndex++,
                PageNumber = pageNumber,
                SectionTitle = sectionTitle,
                ChunkText = chunkText,
                SourceExcerpt = TrimExcerpt(chunkText),
                Embedding = new Vector(embedding),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }
    }

    private static int? ExtractPageNumber(IngestionChunk<string> chunk)
    {
        if (chunk.HasMetadata && chunk.Metadata.TryGetValue("PageNumber", out var pageNumberValue))
        {
            if (pageNumberValue is int pageNumber)
                return pageNumber;

            if (pageNumberValue is string pageAsString && int.TryParse(pageAsString, CultureInfo.InvariantCulture, out var parsedPage))
                return parsedPage;
        }

        if (!string.IsNullOrWhiteSpace(chunk.Context))
        {
            const string prefix = "Page ";
            var pageMarker = chunk.Context.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);

            if (pageMarker >= 0)
            {
                var start = pageMarker + prefix.Length;
                var end = start;

                while (end < chunk.Context.Length && char.IsDigit(chunk.Context[end]))
                    end++;

                var span = chunk.Context[start..end];
                if (int.TryParse(span, CultureInfo.InvariantCulture, out var parsedPage))
                    return parsedPage;
            }
        }

        return null;
    }

    private int? ResolveChunkPageNumber(IngestionChunk<string> chunk)
        => ExtractPageNumber(chunk) ?? _defaultPageNumber;

    private static string TrimExcerpt(string text, int maxLength = 220)
        => text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "...";
}
