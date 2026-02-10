using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IDocumentoProcesadoRepository
{
    Task<DocumentoProcesado?> GetByIdAsync(int id);
    Task<DocumentoProcesado?> GetByExpedienteAndTipoAsync(int expedienteId, int tipoDocumentoId);
    Task<List<DocumentoProcesado>> GetByExpedienteIdAsync(int expedienteId);
    Task<DocumentoProcesado> CreateAsync(DocumentoProcesado documento);
    Task UpdateAsync(DocumentoProcesado documento);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}