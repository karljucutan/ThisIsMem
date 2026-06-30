using API.Features.Rules.Commands;
using System.ComponentModel;
using System.Text;

namespace API.Features.Rules.MafAgents;

/// <summary>
/// DI-backed tool implementation for rule retrieval used by the AGUI agent.
/// </summary>
public sealed class SearchRulesAgentTool
{
    private readonly SearchRulesCommandHandler _handler;

    public SearchRulesAgentTool(SearchRulesCommandHandler handler)
    {
        _handler = handler;
    }

    [Description("Search the knowledge base for rules matching a query. Returns results with progressive disclosure layers (quick summary, supporting details, full context).")]
    public Task<string> ExecuteSearchRulesTool(
        [Description("Natural-language query describing the user's question or intent (e.g., 'calculation of billdate from its duedate?')")]
        string query,
        [Description("Optional domain filter to scope results to a specific business area (e.g., 'Billing').")]
        string? domain = null,
        [Description("Maximum number of candidate rules to return from the retrieval layer (defaults to 5).")]
        int topResults = 5)
    {
        try
        {
            // Enhancement: add a DisclosureLevel parameter here so the agent can infer Minimal, Standard, or Complete
            // from the user's prompt before calling the handler.
            var command = new SearchRulesCommand(
                Query: query,
                Domain: domain,
                TopResults: topResults
            );

            var results = _handler.Handle(command);

            if (results.Count == 0)
                return Task.FromResult("No business rules found matching the query.");

            var sb = new StringBuilder();
            sb.AppendLine($"Found {results.Count} result(s) for: {query}");

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine();
                sb.AppendLine($"[{i + 1}] {r.AnswerSummary} (Confidence: {r.Confidence})");

                if (r.TopSources.Count > 0)
                {
                    sb.AppendLine("  Sources:");
                    foreach (var src in r.TopSources)
                        sb.AppendLine($"  - {src.RuleId}: {src.Title} [{src.Domain}]");
                }

                if (!string.IsNullOrWhiteSpace(r.Rationale))
                    sb.AppendLine($"  Rationale: {r.Rationale}");

                if (r.SupportingMatches.Count > 0)
                {
                    sb.AppendLine("  Supporting matches:");
                    foreach (var m in r.SupportingMatches)
                        sb.AppendLine($"  - [{m.Heading} / {m.Section}] {m.Quote}");
                }

                if (r.RelatedRuleIds.Count > 0)
                    sb.AppendLine($"  Related rules: {string.Join(", ", r.RelatedRuleIds)}");
            }

            return Task.FromResult(sb.ToString().TrimEnd());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"ERROR: {ex.Message}");
        }
    }
}
