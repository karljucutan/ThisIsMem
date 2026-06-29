using API.Features.Rules.Commands;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace API.Features.Rules.MafTools;

/// <summary>
/// AGUI Agent endpoint for SearchRules tool invocation.
/// Creates and maps an AI agent with the SearchRules tool for TanStack AI.
/// </summary>
public static class SearchRulesAgentEndpoint
{
    private const string ToolName = "SearchRules";
    private const string ToolDescription = "Search the knowledge base for rules matching a query. Returns results with progressive disclosure layers (quick summary, supporting details, full context).";

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
                instructions: "You are a helpful assistant that searches business rules from the knowledge base. Use the SearchRules tool to find relevant rules based on user queries.",
                tools: [AIFunctionFactory.Create(ExecuteSearchRulesTool)]);

        // Map the agent to the AGUI endpoint
        app.MapAGUI("/api/agent", agent);
    }

    /// <summary>
    /// Executes the SearchRules tool when invoked by the AI agent.
    /// Called by the agent framework when the tool is selected.
    /// </summary>
    [Description("Search the knowledge base for rules matching a query. Returns results with progressive disclosure layers (quick summary, supporting details, full context).")]
    private static async Task<object> ExecuteSearchRulesTool(
        string query,
        string? domain = null,
        int topResults = 5,
        SearchRulesCommandHandler? handler = null)
    {
        if (handler == null)
        {
            return new { Success = false, Message = "Handler not available" };
        }

        try
        {
            var command = new SearchRulesCommand(
                Query: query,
                Domain: domain,
                TopResults: topResults
            );

            var results = handler.Handle(command);

            return new
            {
                Success = true,
                Data = results,
                Message = $"Found {results.Count} matching rules"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Success = false,
                Message = $"Error searching rules: {ex.Message}"
            };
        }
    }
}
