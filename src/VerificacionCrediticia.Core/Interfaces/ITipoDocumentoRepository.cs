using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface ITipoDocumentoRepository
{
    Task<TipoDocumento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TipoDocumento?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<List<TipoDocumento>> GetActivosAsync(CancellationToken cancellationToken = default);
    Task<List<TipoDocumento>> GetObligatoriosAsync(CancellationToken cancellationToken = default);
    Task<List<TipoDocumento>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TipoDocumento> CreateAsync(TipoDocumento tipoDocumento, CancellationToken cancellationToken = default);
    Task UpdateAsync(TipoDocumento tipoDocumento, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
