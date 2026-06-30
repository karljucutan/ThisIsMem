using API.Features.Rules.Commands;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text;

namespace API.Features.Rules.MafTools;

/// <summary>
/// AGUI Agent endpoint for SearchRules tool invocation.
/// Creates and maps an AI agent with the SearchRules tool for TanStack AI.
/// </summary>
public static class SearchRulesAgentEndpoint
{
    /// <summary>
    /// Maps the AGUI agent endpoint with the SearchRules tool.
    /// </summary>
    public static void MapSearchRulesAgent(this WebApplication app)
    {
        // Get configuration
        string endpoint = app.Configuration["AZURE_OPENAI_ENDPOINT"]
           ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        string deploymentName = app.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
           ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

        // Create the AI agent with Azure OpenAI
        AIAgent agent = new AIProjectClient(
            new Uri(endpoint),
            new DefaultAzureCredential())
            .AsAIAgent(
                model: deploymentName,
                name: "RulesAssistant",
                instructions: @"You are an expert Business Rules Assistant.

                CRITICAL:
                - You must exclusively use the SearchRules tool to find applicable business rules.
                - Never assume, invent, or infer rules that are not present in tool results.
                - Treat SearchRules results as keyword-retrieved candidates, not guaranteed final truth.

                Retrieval-Aware Reasoning Policy:
                - Use only the SearchRules results as evidence; do not consult external sources.
                - Do NOT modify, rewrite, or alter the user's original query when calling SearchRules.
                - Perform semantic reranking in reasoning over returned candidates: assess concept/intent match rather than raw keyword overlap.
                - Prioritize candidates that directly answer the specific variable, formula, relationship, condition, or constraint asked.
                - Prefer precision: return only the most relevant 1–3 rules; exclude tangential keyword matches.
                - If relevance is ambiguous or conflicting, state the uncertainty clearly and ask a single concise clarifying question.
                - If no sufficiently relevant rule exists within the returned candidates, respond exactly: No business rule found for this scenario.

                Response Format:
                - First sentence: state the best applicable rule identifier and a one-line direct answer (example: Rule-106: The BillDate is 30 days before DueDate).
                - For each returned rule (max 1–3):
                    1) Rule ID and title
                    2) Direct answer (1 sentence)
                    3) One-sentence justification explaining why this rule matches the user's intent
                    4) Source citation with file path
                - Keep tone neutral, precise, and implementation-focused.
                - Break complex logic into short bullet points.
                - Always include traceability (rule id, title, and source path).

                Fallback:
                - If no sufficiently relevant rule is found, respond exactly: No business rule found for this scenario.",
                tools: [AIFunctionFactory.Create(ExecuteSearchRulesTool)]);

        // Map the agent to the AGUI endpoint
        app.MapAGUI("/api/agent", agent);
    }

    /// <summary>
    /// Executes the SearchRules tool when invoked by the AI agent.
    /// Returns token-efficient plain text for LLM consumption.
    /// </summary>
    [Description("Search the knowledge base for rules matching a query. Returns results with progressive disclosure layers (quick summary, supporting details, full context).")]
    private static async Task<string> ExecuteSearchRulesTool(
        string query,
        string? domain = null,
        int topResults = 5,
        SearchRulesCommandHandler? handler = null)
    {
        if (handler == null)
            return "ERROR: Handler not available.";

        try
        {
            var command = new SearchRulesCommand(
                Query: query,
                Domain: domain,
                TopResults: topResults
            );

            var results = handler.Handle(command);

            if (results.Count == 0)
                return "No business rules found matching the query.";

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

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }
}
