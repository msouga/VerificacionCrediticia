using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface ITipoDocumentoRepository
{
    Task<TipoDocumento?> GetByIdAsync(int id);
    Task<TipoDocumento?> GetByCodigoAsync(string codigo);
    Task<List<TipoDocumento>> GetActivosAsync();
    Task<List<TipoDocumento>> GetObligatoriosAsync();
    Task<List<TipoDocumento>> GetAllAsync();
    Task<TipoDocumento> CreateAsync(TipoDocumento tipoDocumento);
    Task UpdateAsync(TipoDocumento tipoDocumento);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}