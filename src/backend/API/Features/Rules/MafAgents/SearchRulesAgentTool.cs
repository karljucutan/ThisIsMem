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

            return Task.FromResult(FormatResults($"Found {results.Count} result(s) for: {command.Query}", results));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"ERROR: {ex.Message}");
        }
    }

    [Description("Expand a known rule by exact RuleId using progressive disclosure. Use this for follow-ups such as expand, layer 2, layer 3, acceptance criteria, test cases, examples, or implementation notes.")]
    public Task<string> ExecuteExpandRuleTool(
        [Description("Exact expansion parameters. Pass the exact RuleId and desired DisclosureLevel.")]
        ExpandRuleCommand command)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ExpandRuleCommandHandler>();

            var results = handler.Handle(command);

            if (results.Count == 0)
                return Task.FromResult("No business rules found matching the rule id.");

            return Task.FromResult(FormatResults($"Expanded {command.RuleId} with disclosure {command.DisclosureLevel}", results));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"ERROR: {ex.Message}");
        }
    }

    private static string FormatResults(string header, IReadOnlyList<SearchRulesResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);

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

        return sb.ToString().TrimEnd();
    }
}
