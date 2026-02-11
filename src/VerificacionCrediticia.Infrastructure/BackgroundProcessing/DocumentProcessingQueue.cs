using System.Runtime.CompilerServices;
using System.Threading.Channels;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.BackgroundProcessing;

public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<DocumentoProcesarMessage> _channel = Channel.CreateUnbounded<DocumentoProcesarMessage>(
        new UnboundedChannelOptions { SingleReader = true });

    public async ValueTask EnqueueAsync(DocumentoProcesarMessage message, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public async IAsyncEnumerable<DocumentoProcesarMessage> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }
}
