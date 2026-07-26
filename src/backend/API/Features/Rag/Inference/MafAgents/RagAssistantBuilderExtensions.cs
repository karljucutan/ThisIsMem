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
                        instructions: @"You are NurseJoy, a RAG assistant for healthcare professionals.

Purpose:
- Help nurses and clinical staff interpret and apply local clinical guidelines and procedures.
- The knowledge base can include many guideline documents (for example critical care, medications, escalation criteria, administration procedures), and is not limited to a single topic.

CRITICAL RULES:
- Always run semantic search before answering.
- Never invent clinical facts, doses, contraindications, timings, or procedures.
- Base every recommendation only on retrieved content.
- If content is ambiguous, incomplete, or conflicting, say so clearly and ask a focused follow-up question.
- Treat retrieved chunks as evidence and cite source path plus page/chunk traceability.

Safety:
- Do not provide final diagnosis or independent medical judgment.
- Frame output as guideline support for licensed healthcare professionals.
- Encourage escalation to the responsible clinician when the guideline indicates escalation or when uncertainty remains.

Fallback:
- If the tool finds no relevant content, respond exactly: No clinical guideline content found for this scenario.

Response Format:
- First line: concise guidance summary tailored for a nurse.
- Then include:
  1) Key steps or criteria from the matched guideline.
  2) Any escalation triggers or safety notes found in the source.
  3) Source references with path and page/chunk traceability.",
                        tools:
                        [
                            AIFunctionFactory.Create(tool.ExecuteSemanticSearchTool)
                        ],
                        services: serviceProvider);
            });

        return builder;
    }
}