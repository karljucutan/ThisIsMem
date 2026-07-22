using API.Features.Rag.Shared;
using Microsoft.Extensions.DataIngestion;

namespace API.Features.Rag.Ingestion;

public sealed class RagIngestionDocumentReader : IngestionDocumentReader
{
    private readonly RagPdfReader _pdfReader;

    public RagIngestionDocumentReader(RagPdfReader pdfReader)
    {
        _pdfReader = pdfReader;
    }

    public override async Task<IngestionDocument> ReadAsync(
        FileInfo source,
        string identifier,
        string? mediaType,
        CancellationToken cancellationToken)
    {
        var pages = await _pdfReader.ReadPagesAsync(source.FullName, cancellationToken);
        var document = new IngestionDocument(identifier);

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
                continue;

            var section = new IngestionDocumentSection();
            var header = new IngestionDocumentHeader($"## Page {page.PageNumber}")
            {
                Level = 2,
                Text = $"Page {page.PageNumber}",
                PageNumber = page.PageNumber,
            };
            var paragraph = new IngestionDocumentParagraph(page.Text)
            {
                Text = page.Text,
                PageNumber = page.PageNumber,
            };

            section.Elements.Add(header);
            section.Elements.Add(paragraph);
            document.Sections.Add(section);
        }

        return document;
    }

    public override Task<IngestionDocument> ReadAsync(
        Stream source,
        string identifier,
        string? mediaType,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Stream-based ingestion is not supported for this reader.");
    }
}
