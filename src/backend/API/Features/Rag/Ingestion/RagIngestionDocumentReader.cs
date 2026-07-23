using API.Features.Rag.Shared;
using Microsoft.Extensions.DataIngestion;
using System.Text.RegularExpressions;

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
        if (Path.GetExtension(source.FullName).Equals(".md", StringComparison.OrdinalIgnoreCase))
            return await ReadMarkdownAsync(source.FullName, identifier, cancellationToken);

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

    private static async Task<IngestionDocument> ReadMarkdownAsync(
        string filePath,
        string identifier,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var document = new IngestionDocument(identifier);
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        IngestionDocumentSection? currentSection = null;
        var paragraphLines = new List<string>();

        foreach (var line in lines)
        {
            var headerMatch = Regex.Match(line, "^(#{1,6})\\s+(.+)$");
            if (headerMatch.Success)
            {
                FlushParagraph(currentSection, paragraphLines);

                currentSection = new IngestionDocumentSection();
                var headerText = headerMatch.Groups[2].Value.Trim();
                var header = new IngestionDocumentHeader(line)
                {
                    Level = headerMatch.Groups[1].Value.Length,
                    Text = headerText,
                    PageNumber = TryExtractPageNumber(headerText),
                };

                currentSection.Elements.Add(header);
                document.Sections.Add(currentSection);
                continue;
            }

            if (currentSection is null)
            {
                currentSection = new IngestionDocumentSection();
                document.Sections.Add(currentSection);
            }

            paragraphLines.Add(line);
        }

        FlushParagraph(currentSection, paragraphLines);
        return document;
    }

    private static void FlushParagraph(IngestionDocumentSection? section, List<string> paragraphLines)
    {
        if (section is null || paragraphLines.Count == 0)
            return;

        var text = string.Join("\n", paragraphLines).Trim();
        paragraphLines.Clear();

        if (string.IsNullOrWhiteSpace(text))
            return;

        var paragraph = new IngestionDocumentParagraph(text)
        {
            Text = text,
            PageNumber = TryExtractPageNumber(section),
        };

        section.Elements.Add(paragraph);
    }

    private static int? TryExtractPageNumber(IngestionDocumentSection section)
        => section.Elements.OfType<IngestionDocumentHeader>().Select(x => x.PageNumber).FirstOrDefault();

    private static int? TryExtractPageNumber(string headerText)
    {
        var pageMatch = Regex.Match(headerText, "^Page\\s+(\\d+)$", RegexOptions.IgnoreCase);
        if (!pageMatch.Success)
            return null;

        return int.TryParse(pageMatch.Groups[1].Value, out var pageNumber) ? pageNumber : null;
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
