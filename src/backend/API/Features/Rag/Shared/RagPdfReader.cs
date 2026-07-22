using API.Infrastructure.Options;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Microsoft.Extensions.Options;
using System.Text;

namespace API.Features.Rag.Shared;

public sealed class RagPdfReader
{
    private readonly RagOptions _options;

    public RagPdfReader(IOptions<RagOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<RagPdfPage>> ReadPagesAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"RAG source PDF was not found: {filePath}", filePath);

        if (string.IsNullOrWhiteSpace(_options.DocumentIntelligenceEndpoint))
            throw new InvalidOperationException("Rag:DocumentIntelligenceEndpoint is not configured.");

        var endpoint = new Uri(_options.DocumentIntelligenceEndpoint);
        var client = CreateClient(endpoint);

        await using var stream = File.OpenRead(filePath);
        var content = await BinaryData.FromStreamAsync(stream, cancellationToken);

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            _options.DocumentIntelligenceModelId,
            content,
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var pages = new List<RagPdfPage>(result.Pages.Count);

        foreach (var page in result.Pages)
        {
            var text = ExtractPageText(page);
            pages.Add(new RagPdfPage(page.PageNumber, text));
        }

        return pages;
    }

    private static DocumentIntelligenceClient CreateClient(Uri endpoint)
        => new(endpoint, new DefaultAzureCredential());

    private static string ExtractPageText(DocumentPage page)
    {
        if (page.Lines.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var line in page.Lines)
        {
            if (!string.IsNullOrWhiteSpace(line.Content))
                builder.AppendLine(line.Content);
        }

        return builder.ToString().Trim();
    }
}