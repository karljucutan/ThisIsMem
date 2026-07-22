using System.ComponentModel;
using System.Text;

namespace API.Features.Rag.Inference.MafAgents;

public sealed class RagSemanticSearchAgentTool
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RagSemanticSearchAgentTool(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [Description("Search the incident-response PDF semantically and return the most relevant chunks with traceability.")]
    public async Task<string> ExecuteSemanticSearchTool(
        [Description("Structured semantic search parameters.")] SemanticSearchCommand command)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<RagSemanticSearchService>();
        var results = await service.SearchAsync(command.Query, command.TopResults, CancellationToken.None);

        if (results.Count == 0)
            return "No incident-response content found matching the query.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} semantic match(es) for: {command.Query}");

        foreach (var result in results)
        {
            sb.AppendLine();
            sb.AppendLine($"- {result.Title} ({result.DocumentKey})");
            sb.AppendLine($"  Source: {result.SourcePath}");
            sb.AppendLine($"  Page: {result.PageNumber?.ToString() ?? "n/a"}, Chunk: {result.ChunkIndex}, Distance: {result.Distance:F4}");
            sb.AppendLine($"  Excerpt: {result.ChunkText}");
        }

        return sb.ToString().TrimEnd();
    }
}