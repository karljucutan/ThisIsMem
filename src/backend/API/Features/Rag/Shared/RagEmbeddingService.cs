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

        var foundryClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
        var embeddingClient = foundryClient.ProjectOpenAIClient.GetEmbeddingClient(options.Value.EmbeddingDeploymentName);
        _embeddingGenerator = embeddingClient.AsIEmbeddingGenerator();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string content, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(content, cancellationToken: cancellationToken);
        return embedding.Vector.ToArray();
    }
}