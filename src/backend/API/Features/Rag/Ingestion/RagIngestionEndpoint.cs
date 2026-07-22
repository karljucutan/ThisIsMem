using API.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace API.Features.Rag.Ingestion;

public static class RagIngestionEndpoint
{
    public static void MapRagIngestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/rag/ingestion/reindex", HandleReindex)
            .WithName("ReindexIncidentResponseRag")
            .Produces(StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> HandleReindex(
        RagIngestionQueue queue,
        IOptions<KnowledgeBaseProceduresOptions> options,
        CancellationToken cancellationToken)
    {
        await queue.EnqueueAsync(new RagIngestionRequest(options.Value.DocumentKey, options.Value.IncidentResponsePdfPath), cancellationToken);
        return Results.Accepted("/api/rag/ingestion/reindex", new { message = "RAG ingestion queued." });
    }
}