using API.Infrastructure.Options;
using API.Infrastructure.Persistence;
using API.Features.Rag.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace API.Features.Rag.Inference;

public sealed class RagSemanticSearchService
{
    private readonly MemDbContext _dbContext;
    private readonly RagEmbeddingService _embeddingService;
    private readonly KnowledgeBaseProceduresOptions _options;

    public RagSemanticSearchService(MemDbContext dbContext, RagEmbeddingService embeddingService, IOptions<KnowledgeBaseProceduresOptions> options)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<RagSemanticSearchResult>> SearchAsync(string query, int topResults, CancellationToken cancellationToken)
    {
        var queryEmbedding = new Vector(await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken));

        return await (
            from chunk in _dbContext.Chunks.AsNoTracking()
            join document in _dbContext.Documents.AsNoTracking()
                on chunk.RagDocumentId equals document.Id
            where document.DocumentKey == _options.DocumentKey
            orderby chunk.Embedding.CosineDistance(queryEmbedding)
            select new RagSemanticSearchResult
            {
                DocumentKey = document.DocumentKey,
                Title = document.Title,
                SourcePath = document.SourcePath,
                ChunkIndex = chunk.ChunkIndex,
                PageNumber = chunk.PageNumber,
                SectionTitle = chunk.SectionTitle,
                ChunkText = chunk.ChunkText,
                Distance = chunk.Embedding.CosineDistance(queryEmbedding)
            })
            .Take(topResults)
            .ToListAsync(cancellationToken);
    }
}