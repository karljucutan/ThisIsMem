using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

namespace API.Features.Rag.Inference.MafAgents;

public static class RagAssistantEndpoint
{
    public static void MapRagAssistantAIAgent(this WebApplication app)
    {
        app.MapAGUI(agentName: "RagAssistantAIAgent", pattern: "/api/rag/agent")
            .WithName("RagAssistantAIAgent");
    }
}