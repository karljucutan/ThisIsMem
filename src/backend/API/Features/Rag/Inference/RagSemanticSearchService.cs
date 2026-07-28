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
            // Optional: add a documentKeys (string[]) parameter to scope search to specific documents.
            // Use when: (1) user explicitly selects a procedure/guideline to search within,
            // (2) role-based access control limits which documents a user may see,
            // (3) two documents conflict and the caller must pin a source.
            // Without this filter, cosine distance ranking already surfaces the most relevant
            // chunks across all ingested documents — prefer that for general queries.


            // To support filtering: add a GetAvailableDocumentKeys agent tool that queries
            // rag_document for all DocumentKey + Title pairs so the LLM can pick the right
            // keys before calling this search. 
            // Pattern: agent calls GetDocumentKeys first, then passes selected keys into SearchAsync.
            orderby chunk.Embedding.CosineDistance(queryEmbedding) //Why order by cosine distance? Because smaller distance means more similar meaning.
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