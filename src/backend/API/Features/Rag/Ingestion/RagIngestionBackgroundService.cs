using API.Infrastructure.Persistence;

namespace API.Features.Rag.Ingestion;

public sealed class RagIngestionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RagIngestionQueue _queue;

    public RagIngestionBackgroundService(IServiceScopeFactory scopeFactory, RagIngestionQueue queue)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<RagIngestionService>();

            try
            {
                await service.RebuildDocumentAsync(request, stoppingToken);
            }
            catch
            {
                // Keep the worker alive; ingestion failures are surfaced through logs in a later pass.
            }
        }
    }
}