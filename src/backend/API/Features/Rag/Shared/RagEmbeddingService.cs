using API.Infrastructure.Options;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace API.Features.Rag.Shared;

public sealed class RagEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public RagEmbeddingService(IConfiguration configuration, IOptions<RagOptions> options)
    {
        var endpoint = configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

        var embeddingDeploymentName = configuration["AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME"];

        if (string.IsNullOrWhiteSpace(embeddingDeploymentName))
            embeddingDeploymentName = options.Value.EmbeddingDeploymentName;

        if (string.IsNullOrWhiteSpace(embeddingDeploymentName))
            throw new InvalidOperationException("AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME or Rag:EmbeddingDeploymentName must be set.");

        var foundryClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
        var embeddingClient = foundryClient.ProjectOpenAIClient.GetEmbeddingClient(embeddingDeploymentName);
        _embeddingGenerator = embeddingClient.AsIEmbeddingGenerator();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string content, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(content, cancellationToken: cancellationToken);
        return embedding.Vector.ToArray();
    }
}