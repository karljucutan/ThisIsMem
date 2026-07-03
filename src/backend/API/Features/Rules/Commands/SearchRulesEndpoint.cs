using Microsoft.AspNetCore.OpenApi;

namespace API.Features.Rules.Commands;

/// <summary>
/// Endpoint for searching rules. REST endpoint for manual testing.
/// </summary>
public static class SearchRulesEndpoint
{
    /// <summary>
    /// Maps the search rules endpoint to the application.
    /// </summary>
    public static void MapSearchRulesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/rules/search", HandleSearchRules)
            .WithName("SearchRules")
            .Produces<List<SearchRulesResult>>(StatusCodes.Status200OK)
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Search rules by content";
                operation.Description = "Search the knowledge base for rules matching the query. Default disclosure is Layer 1 (quick answer); Layer 2 and Layer 3 are explicit opt-ins via DisclosureLevel.";
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Handles the search rules query.
    /// </summary>
    private static IResult HandleSearchRules(SearchRulesCommand query, SearchRulesCommandHandler handler)
    {
        var results = handler.Handle(query);
        return Results.Ok(results);
    }
}
