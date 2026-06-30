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
        //// Get configuration
        //string endpoint = app.Configuration["AZURE_OPENAI_ENDPOINT"]
        //    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        //string deploymentName = app.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
        //    ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

        //// Create the AI agent with Azure OpenAI
        //AIAgent agent = new AIProjectClient(
        //        new Uri(endpoint),
        //        new DefaultAzureCredential())
        //    .AsAIAgent(
        //        model: deploymentName,
        //        name: "RulesAssistant",
        //        instructions: @"You are an expert Business Rules Assistant.

        //        CRITICAL: You must exclusively use the SearchRules tool to find relevant business rules, logic, and constraints. Never assume or invent a rule.

        //        Guidelines:
        //        - State the applicable rule or constraint directly in the first sentence, including the rule identifier (e.g., Rule-101).
        //        - Break down complex multi-step logical conditions into clear bullet points.
        //        - Bold key conditions, variables, and parameters (e.g., **If X > Y**, **Maximum Limit**).
        //        - Keep a neutral, precise, and logical tone—avoid interpretation or opinion.
        //        - When multiple rules apply, prioritize by relevance and clearly separate each rule.
        //        - Always cite the source rule (e.g., 'Rule-101: Minimum Down Payment').
        //        - If the SearchRules tool returns no matching criteria, state: 'No business rule found for this scenario.'",
        //        tools: [AIFunctionFactory.Create(ExecuteSearchRulesTool)]);

        //// Map the agent to the AGUI endpoint
        //app.MapAGUI("/api/agent", agent);
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
