using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IExpedienteRepository
{
    Task<Expediente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Expediente?> GetByIdTrackingAsync(int id, CancellationToken cancellationToken = default);
    Task<Expediente?> GetByIdWithDocumentosAsync(int id, CancellationToken cancellationToken = default);
    Task<Expediente?> GetByIdWithDocumentosTrackingAsync(int id, CancellationToken cancellationToken = default);
    Task<Expediente?> GetByDniAsync(string dni, CancellationToken cancellationToken = default);
    Task<Expediente> CreateAsync(Expediente expediente, CancellationToken cancellationToken = default);
    Task UpdateAsync(Expediente expediente, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Expediente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<Expediente> Items, int Total)> GetPaginadoAsync(int pagina, int tamanoPagina, CancellationToken cancellationToken = default);
    Task<(List<ExpedienteResumenDto> Items, int Total)> GetPaginadoResumenAsync(int pagina, int tamanoPagina, List<int> tipoDocumentoObligatorioIds, CancellationToken cancellationToken = default);
}
