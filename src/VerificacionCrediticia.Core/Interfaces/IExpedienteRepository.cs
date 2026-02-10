using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IExpedienteRepository
{
    Task<Expediente?> GetByIdAsync(int id);
    Task<Expediente?> GetByIdWithDocumentosAsync(int id);
    Task<Expediente?> GetByDniAsync(string dni);
    Task<Expediente> CreateAsync(Expediente expediente);
    Task UpdateAsync(Expediente expediente);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<List<Expediente>> GetAllAsync();
}