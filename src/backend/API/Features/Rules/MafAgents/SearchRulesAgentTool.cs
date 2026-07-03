using API.Features.Rules.Commands;
using System.ComponentModel;
using System.Text;

namespace API.Features.Rules.MafAgents;

/// <summary>
/// DI-backed tool implementation for rule retrieval used by the AGUI agent.
/// Registered as singleton; creates a short-lived scope per invocation to resolve scoped dependencies.
/// </summary>
public sealed class SearchRulesAgentTool
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SearchRulesAgentTool(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [Description("Search the knowledge base for rules using a structured SearchRulesCommand. Default disclosure is Layer 1 (quick summary), with Layer 2 and Layer 3 available when explicitly requested.")]
    public Task<string> ExecuteSearchRulesTool(
        [Description("Structured search parameters. The AI will also see field-level descriptions for Query, Domain, TopResults, and DisclosureLevel.")]
        SearchRulesCommand command)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<SearchRulesCommandHandler>();

            var results = handler.Handle(command);

            if (results.Count == 0)
                return Task.FromResult("No business rules found matching the query.");

            var sb = new StringBuilder();
            sb.AppendLine($"Found {results.Count} result(s) for: {command.Query}");

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine();

                sb.AppendLine($"{i + 1}. {r.TopSources.FirstOrDefault()?.RuleId ?? "Unknown rule"}: {r.TopSources.FirstOrDefault()?.Title ?? "Untitled rule"}");
                sb.AppendLine($"   Answer: {r.AnswerSummary}");

                if (r.TopSources.Count > 0)
                {
                    sb.AppendLine("  Sources:");
                    foreach (var src in r.TopSources)
                        sb.AppendLine($"  - {src.RuleId}: {src.Title} [{src.Domain}] {src.FilePath}");
                }

                if (!string.IsNullOrWhiteSpace(r.Rationale))
                    sb.AppendLine($"  Rationale: {r.Rationale}");

                if (r.SupportingMatches.Count > 0)
                {
                    sb.AppendLine("  Supporting matches:");
                    foreach (var m in r.SupportingMatches)
                        sb.AppendLine($"  - [{m.Heading} / {m.Section}] {m.Quote} ({m.SourcePath})");
                }

                if (r.RelatedRuleIds.Count > 0)
                    sb.AppendLine($"  Related rules: {string.Join(", ", r.RelatedRuleIds)}");

                if (r.RuleMetadata.Count > 0)
                {
                    sb.AppendLine("  Metadata:");
                    foreach (var metadata in r.RuleMetadata)
                        sb.AppendLine($"  - {metadata.Id}: {metadata.Title} [{metadata.Domain}] v{metadata.Version}");
                }

                if (!string.IsNullOrWhiteSpace(r.FullSourceMarkdown))
                {
                    sb.AppendLine("  Full source markdown:");
                    sb.AppendLine(r.FullSourceMarkdown);
                }
            }

            return Task.FromResult(sb.ToString().TrimEnd());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"ERROR: {ex.Message}");
        }
    }
}
