using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IReglaEvaluacionRepository
{
    Task<ReglaEvaluacion?> GetByIdAsync(int id);
    Task<List<ReglaEvaluacion>> GetActivasAsync();
    Task<List<ReglaEvaluacion>> GetAllAsync();
    Task<ReglaEvaluacion> CreateAsync(ReglaEvaluacion regla);
    Task UpdateAsync(ReglaEvaluacion regla);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}