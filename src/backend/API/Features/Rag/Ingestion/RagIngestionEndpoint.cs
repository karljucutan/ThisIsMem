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
        RagIngestionQueue queue,
        IOptions<KnowledgeBaseProceduresOptions> options,
        CancellationToken cancellationToken)
    {
        await queue.EnqueueAsync(new RagIngestionRequest(options.Value.DocumentKey, options.Value.IncidentResponsePdfPath), cancellationToken);
        return Results.Accepted("/api/rag/ingestion/ingest", new { message = "RAG first ingestion queued from PDF source." });
    }

    private static async Task<IResult> HandleReindex(
        RagIngestionQueue queue,
        IOptions<KnowledgeBaseProceduresOptions> options,
        CancellationToken cancellationToken)
    {
        var markdownPath = Path.ChangeExtension(options.Value.IncidentResponsePdfPath, ".md");
        await queue.EnqueueAsync(new RagIngestionRequest(options.Value.DocumentKey, markdownPath), cancellationToken);
        return Results.Accepted("/api/rag/ingestion/reindex", new { message = "RAG reindex queued from markdown source." });
    }
}