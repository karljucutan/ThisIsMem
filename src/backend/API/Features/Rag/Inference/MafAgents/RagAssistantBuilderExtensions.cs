using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace API.Features.Rag.Inference.MafAgents;

public static class RagAssistantBuilderExtensions
{
    public static WebApplicationBuilder AddRagAssistantAIAgent(this WebApplicationBuilder builder)
    {
        string endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        string deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is not set.");

        builder.Services.AddSingleton<RagSemanticSearchAgentTool>();

        builder.AddAIAgent(
            name: "RagAssistantAIAgent",
            (serviceProvider, key) =>
            {
                var tool = serviceProvider.GetRequiredService<RagSemanticSearchAgentTool>();

                return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                    .AsAIAgent(
                        model: deploymentName,
                        name: key,
                        instructions: @"You are an incident-response RAG assistant.

CRITICAL:
- Always use the semantic search tool before answering.
- Never invent incident-response steps or details.
- Treat tool output as evidence and cite the source path.

Fallback:
- If the tool finds no relevant content, respond exactly: No incident-response content found for this scenario.

Response Format:
- First line: direct answer or best match summary.
- Then cite the source path and page/chunk traceability.",
                        tools:
                        [
                            AIFunctionFactory.Create(tool.ExecuteSemanticSearchTool)
                        ],
                        services: serviceProvider);
            });

        return builder;
    }
}