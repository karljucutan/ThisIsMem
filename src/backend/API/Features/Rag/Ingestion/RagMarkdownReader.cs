using Microsoft.Extensions.DataIngestion;
using System.Text;

namespace API.Features.Rag.Ingestion;

/// <summary>
/// Reads markdown files as raw text, preserving all content including HTML elements (e.g. tables
/// produced by Azure Document Intelligence). Unlike the built-in MarkdownReader, this reader does
/// not parse or strip any markup — the file content is returned verbatim so the downstream chunker
/// receives the full, unmodified text.
/// </summary>
public sealed class RagMarkdownReader : IngestionDocumentReader
{
    public override async Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(source.FullName, Encoding.UTF8, cancellationToken);
        return BuildDocument(identifier, content);
    }

    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(source, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return BuildDocument(identifier, content);
    }

    private static IngestionDocument BuildDocument(string identifier, string content)
    {
        var document = new IngestionDocument(identifier);
        var section = new IngestionDocumentSection();
        section.Elements.Add(new IngestionDocumentParagraph(content));
        document.Sections.Add(section);
        return document;
    }
}