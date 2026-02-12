using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface INarrativaEvaluacionService
{
    Task<string> GenerarNarrativaAsync(
        Expediente expediente,
        ResultadoMotorReglas resultado,
        Dictionary<string, object> datosEvaluacion,
        CancellationToken cancellationToken = default);
}
