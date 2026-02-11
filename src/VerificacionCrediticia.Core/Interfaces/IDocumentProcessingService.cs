using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IDocumentProcessingService
{
    Task<object> ProcesarSegunTipoAsync(
        string codigoTipo, Stream documentStream, string fileName,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null);

    Task ActualizarDatosExpedienteAsync(Expediente expediente, string codigoTipo, object resultado);

    decimal? ObtenerConfianzaDeResultado(object resultado);
}
