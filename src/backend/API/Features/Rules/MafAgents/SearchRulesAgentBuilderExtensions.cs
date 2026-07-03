using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace API.Features.Rules.MafAgents;

public static class SearchRulesAgentBuilderExtensions
{
    public static WebApplicationBuilder AddRuleAssistantAIAgent(this WebApplicationBuilder builder)
    {
        string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

        builder.Services.AddSingleton<SearchRulesAgentTool>();

        builder.AddAIAgent(
            name: "RulesAssistantAIAgent",
            (serviceProvider, key) =>
            {
                var tool = serviceProvider.GetRequiredService<SearchRulesAgentTool>();

                //// Uncomment for shorter, simpler instructions for lesser token usage for testing purposes.
                //return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                //    .AsAIAgent(
                //        model: deploymentName,
                //        name: key,
                //        instructions: "You are a business-rules assistant. Always use the provided function tool for rule lookup, and do not invent rules. If no relevant rule is found from the tool result, reply exactly: No business rule found for this scenario.",
                //        tools:
                //        [
                //            AIFunctionFactory.Create(tool.ExecuteSearchRulesTool)
                //        ],
                //        services: serviceProvider
                //    );

                return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                    .AsAIAgent(
                        model: deploymentName,
                        name: key,
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
                        tools: [AIFunctionFactory.Create(tool.ExecuteSearchRulesTool)],
                        services: serviceProvider // Gives the tool access to scoped DI dependencies
                    );

            });

        return builder;
    }
}