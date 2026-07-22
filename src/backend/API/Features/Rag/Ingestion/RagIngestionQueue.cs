using System.Threading.Channels;

namespace API.Features.Rag.Ingestion;

public sealed class RagIngestionQueue
{
    private readonly Channel<RagIngestionRequest> _channel = Channel.CreateUnbounded<RagIngestionRequest>();

    public ValueTask EnqueueAsync(RagIngestionRequest request, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(request, cancellationToken);

    public IAsyncEnumerable<RagIngestionRequest> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}