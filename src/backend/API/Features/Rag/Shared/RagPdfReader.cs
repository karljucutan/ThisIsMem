using UglyToad.PdfPig;

namespace API.Features.Rag.Shared;

public static class RagPdfReader
{
    public static IReadOnlyList<RagPdfPage> ReadPages(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"RAG source PDF was not found: {filePath}", filePath);

        using var pdf = PdfDocument.Open(filePath);

        return [.. pdf.GetPages().Select(page => new RagPdfPage(page.Number, page.Text))];
    }
}