using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IDocumentoProcesadoRepository
{
    Task<DocumentoProcesado?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DocumentoProcesado?> GetByExpedienteAndTipoAsync(int expedienteId, int tipoDocumentoId, CancellationToken cancellationToken = default);
    Task<List<DocumentoProcesado>> GetByExpedienteIdAsync(int expedienteId, CancellationToken cancellationToken = default);
    Task<DocumentoProcesado> CreateAsync(DocumentoProcesado documento, CancellationToken cancellationToken = default);
    Task UpdateAsync(DocumentoProcesado documento, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
