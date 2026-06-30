using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

namespace API.Features.Rules.MafAgents;

/// <summary>
/// AGUI Agent endpoint for SearchRules tool invocation.
/// Maps an already-registered AI agent for TanStack AI.
/// </summary>
public static class SearchRulesAgentEndpoint
{
    /// <summary>
    /// Maps the AGUI agent endpoint.
    /// </summary>
    public static void MapRulesAssistantAIAgent(this WebApplication app)
    {
        // app.MapAGUI("/api/agent", agent)
        app.MapAGUI(agentName: "RulesAssistantAIAgent", pattern: "/api/agent")
        .WithName("RulesAssistantAIAgent")
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Summary = "Agent search rules by content";
            operation.Description = "Search the knowledge base for rules matching the query. Returns results with progressive disclosure layers: Layer 1 (quick answer), Layer 2 (supporting details), Layer 3 (full context).";
            return Task.CompletedTask;
        });
    }
}
