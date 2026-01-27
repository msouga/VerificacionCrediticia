using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IVerificacionService
{
    Task<ResultadoEvaluacionDto> EvaluarSolicitudAsync(
        SolicitudVerificacionDto solicitud,
        CancellationToken cancellationToken = default);
}
