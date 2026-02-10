using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IExpedienteService
{
    Task<ExpedienteDto> CrearExpedienteAsync(CrearExpedienteRequest request);
    Task<ExpedienteDto?> GetExpedienteAsync(int id);
    Task<object> ProcesarDocumentoAsync(int expedienteId, string codigoTipo, Stream documentStream, string fileName, IProgress<string>? progreso = null);
    Task<object> ReemplazarDocumentoAsync(int expedienteId, int documentoId, Stream documentStream, string fileName, IProgress<string>? progreso = null);
    Task<ExpedienteDto> EvaluarExpedienteAsync(int expedienteId);
    Task<List<TipoDocumentoDto>> GetTiposDocumentoAsync();
}