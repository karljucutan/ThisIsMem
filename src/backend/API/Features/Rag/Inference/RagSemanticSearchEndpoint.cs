namespace API.Features.Rag.Inference;

public static class RagSemanticSearchEndpoint
{
    public static void MapRagSemanticSearchEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/rag/search", HandleSearch)
            .WithName("SemanticSearchRag")
            .Produces<List<RagSemanticSearchResult>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleSearch(
        SemanticSearchCommand command,
        RagSemanticSearchService service,
        CancellationToken cancellationToken)
    {
        var results = await service.SearchAsync(command.Query, command.TopResults, cancellationToken);
        return Results.Ok(results);
    }
}