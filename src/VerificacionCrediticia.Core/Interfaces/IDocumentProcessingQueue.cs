using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IDocumentProcessingQueue
{
    ValueTask EnqueueAsync(DocumentoProcesarMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<DocumentoProcesarMessage> ReadAllAsync(CancellationToken cancellationToken);
}
