using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IReglaEvaluacionRepository
{
    Task<ReglaEvaluacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ReglaEvaluacion>> GetActivasAsync(CancellationToken cancellationToken = default);
    Task<List<ReglaEvaluacion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReglaEvaluacion> CreateAsync(ReglaEvaluacion regla, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReglaEvaluacion regla, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
