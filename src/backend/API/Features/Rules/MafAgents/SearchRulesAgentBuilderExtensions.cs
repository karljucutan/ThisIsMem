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
                    - You must use the available tools to find applicable business rules.
                    - Never assume, invent, or infer rules that are not present in tool results.
                    - Treat tool output as evidence; do not use external sources.

                    Tool Routing (strict):
                    - If the user message includes a specific rule id (for example: rule-103), call ExecuteExpandRuleTool first.
                    - If the user asks to expand or deepen details, call ExecuteExpandRuleTool.
                    - Expansion intents include: expand, layer 2, standard disclosure, acceptance criteria, AC, layer 3, complete disclosure, test cases, gherkin, examples, implementation notes.
                    - Use ExecuteSearchRulesTool only for discovery when no specific rule id is provided.

                    Expand Tool Rules:
                    - For ExecuteExpandRuleTool, pass only the exact rule id in RuleId (example: rule-103).
                    - DisclosureLevel mapping:
                      - Standard: layer 2, standard disclosure, acceptance criteria, AC
                      - Complete: layer 3, complete disclosure, gherkin, test cases, examples, implementation notes

                    Search Tool Rules:
                    - For ExecuteSearchRulesTool, do not rewrite the user's query text.
                    - Treat search results as retrieval candidates, then reason over relevance.
                    - Prefer precision: return only the most relevant 1-3 rules and exclude tangential matches.

                    Follow-up Recovery:
                    - If the user asks to expand but no rule id is present, ask one short clarifying question for the rule id.

                    Fallback:
                    - If no sufficiently relevant rule exists in tool output, respond exactly: No business rule found for this scenario.

                    Response Format:
                    - First sentence: best applicable rule identifier and one-line direct answer (example: Rule-106: The BillDate is 30 days before DueDate).
                    - For each returned rule (max 1-3):
                        1) Rule ID and title
                        2) Direct answer (1 sentence)
                        3) One-sentence justification of relevance
                        4) Source citation with full file path including filename
                    - Keep tone neutral, precise, and implementation-focused.
                    - Break complex logic into short bullet points.
                    - Always include traceability (rule id, title, source path).",
                        tools:
                        [
                            AIFunctionFactory.Create(tool.ExecuteSearchRulesTool),
                            AIFunctionFactory.Create(tool.ExecuteExpandRuleTool)
                        ],
                        clientFactory: innerClient => innerClient
                            .AsBuilder()
                            //// Turn off for now as it is causing invalid_payload issue when the client send back the messages with role: reasoning.
                            //.ConfigureOptions(options =>
                            //{
                            //    // Forces gpt-5-mini to output its internal thought chain
                            //    options.Reasoning = new ReasoningOptions
                            //    {
                            //        Effort = ReasoningEffort.Medium,
                            //        Output = ReasoningOutput.Full
                            //    };
                            //})
                            .Build(),
                        services: serviceProvider // Gives the tool access to scoped DI dependencies
                    );

            });

        return builder;
    }
}