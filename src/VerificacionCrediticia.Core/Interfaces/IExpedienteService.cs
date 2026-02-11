using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IExpedienteService
{
    Task<ExpedienteDto> CrearExpedienteAsync(CrearExpedienteRequest request);
    Task<ExpedienteDto?> GetExpedienteAsync(int id);
    Task<ListaExpedientesResponse> ListarExpedientesAsync(int pagina, int tamanoPagina);
    Task<ExpedienteDto> ActualizarExpedienteAsync(int id, ActualizarExpedienteRequest request);
    Task EliminarExpedientesAsync(List<int> ids);
    Task<DocumentoProcesadoResumenDto> SubirDocumentoAsync(int expedienteId, string codigoTipo, Stream stream, string fileName);
    Task<DocumentoProcesadoResumenDto> ReemplazarDocumentoSubidoAsync(int expedienteId, int documentoId, Stream stream, string fileName);
    Task<ExpedienteDto> EvaluarExpedienteAsync(int expedienteId, IProgress<ProgresoEvaluacionDto>? progreso = null, CancellationToken cancellationToken = default);
    Task<List<TipoDocumentoDto>> GetTiposDocumentoAsync();
    Task<List<DocumentoProcesadoResumenDto>> SubirDocumentosBulkAsync(int expedienteId, List<(Stream Stream, string FileName)> archivos);
}
