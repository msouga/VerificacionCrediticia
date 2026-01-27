using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public class VerificacionService : IVerificacionService
{
    private readonly IExploradorRedService _exploradorRed;
    private readonly IScoringService _scoringService;

    public VerificacionService(
        IExploradorRedService exploradorRed,
        IScoringService scoringService)
    {
        _exploradorRed = exploradorRed;
        _scoringService = scoringService;
    }

    public async Task<ResultadoEvaluacionDto> EvaluarSolicitudAsync(
        SolicitudVerificacionDto solicitud,
        CancellationToken cancellationToken = default)
    {
        // Validar entrada
        ValidarSolicitud(solicitud);

        // Explorar la red de relaciones
        var resultadoExploracion = await _exploradorRed.ExplorarRedAsync(
            solicitud.DniSolicitante,
            solicitud.RucEmpresa,
            solicitud.ProfundidadMaxima,
            cancellationToken);

        // Evaluar y generar score
        var resultadoEvaluacion = _scoringService.EvaluarRed(resultadoExploracion);

        // Incluir grafo si se solicitó
        if (!solicitud.IncluirDetalleGrafo)
        {
            resultadoEvaluacion.Grafo = null;
        }
        else
        {
            resultadoEvaluacion.Grafo = resultadoExploracion.Grafo;
        }

        return resultadoEvaluacion;
    }

    private void ValidarSolicitud(SolicitudVerificacionDto solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.DniSolicitante))
            throw new ArgumentException("El DNI del solicitante es requerido", nameof(solicitud.DniSolicitante));

        if (solicitud.DniSolicitante.Length != 8 || !solicitud.DniSolicitante.All(char.IsDigit))
            throw new ArgumentException("El DNI debe tener 8 dígitos", nameof(solicitud.DniSolicitante));

        if (string.IsNullOrWhiteSpace(solicitud.RucEmpresa))
            throw new ArgumentException("El RUC de la empresa es requerido", nameof(solicitud.RucEmpresa));

        if (solicitud.RucEmpresa.Length != 11 || !solicitud.RucEmpresa.All(char.IsDigit))
            throw new ArgumentException("El RUC debe tener 11 dígitos", nameof(solicitud.RucEmpresa));

        if (solicitud.ProfundidadMaxima < 1 || solicitud.ProfundidadMaxima > 3)
            throw new ArgumentException("La profundidad máxima debe estar entre 1 y 3", nameof(solicitud.ProfundidadMaxima));
    }
}
