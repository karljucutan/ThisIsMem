using API.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace API.Features.Rag.Ingestion;

public static class RagIngestionEndpoint
{
    public static void MapRagIngestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/rag/ingestion/ingest", HandleFirstIngestion)
            .WithName("IngestIncidentResponseRag")
            .Produces(StatusCodes.Status202Accepted);

        app.MapPost("/api/rag/ingestion/reindex", HandleReindex)
            .WithName("ReindexIncidentResponseRag")
            .Produces(StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> HandleFirstIngestion(
        RagIngestionDocumentRequest request,
        RagIngestionQueue queue,
        IOptions<KnowledgeBaseProceduresOptions> options,
        CancellationToken cancellationToken)
    {
        var document = options.Value.GetDocument(request.DocumentKey);

        if (document is null)
            return Results.NotFound(new { message = $"No procedure document is configured for '{request.DocumentKey}'." });

        await queue.EnqueueAsync(new RagIngestionRequest(document.DocumentKey, document.DocumentPath), cancellationToken);
        return Results.Accepted("/api/rag/ingestion/ingest", new { message = "RAG first ingestion queued from PDF source." });
    }

    private static async Task<IResult> HandleReindex(
        RagIngestionDocumentRequest request,
        RagIngestionQueue queue,
        IOptions<KnowledgeBaseProceduresOptions> options,
        CancellationToken cancellationToken)
    {
        var document = options.Value.GetDocument(request.DocumentKey);

        if (document is null)
            return Results.NotFound(new { message = $"No procedure document is configured for '{request.DocumentKey}'." });

        var markdownPath = Path.ChangeExtension(document.DocumentPath, ".md");
        await queue.EnqueueAsync(new RagIngestionRequest(document.DocumentKey, markdownPath), cancellationToken);
        return Results.Accepted("/api/rag/ingestion/reindex", new { message = "RAG reindex queued from markdown source." });
    }
}