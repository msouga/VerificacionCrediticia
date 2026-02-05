using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VerificacionController : ControllerBase
{
    private readonly IVerificacionService _verificacionService;
    private readonly ILogger<VerificacionController> _logger;

    public VerificacionController(
        IVerificacionService verificacionService,
        ILogger<VerificacionController> logger)
    {
        _verificacionService = verificacionService;
        _logger = logger;
    }

    /// <summary>
    /// Evalua una solicitud de credito analizando la red de relaciones empresariales
    /// </summary>
    [HttpPost("evaluar")]
    [ProducesResponseType(typeof(ResultadoEvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultadoEvaluacionDto>> Evaluar(
        [FromBody] SolicitudVerificacionDto solicitud,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando evaluacion para DNI: {Dni}, RUC: {Ruc}",
            solicitud.DniSolicitante,
            solicitud.RucEmpresa);

        try
        {
            var resultado = await _verificacionService.EvaluarSolicitudAsync(solicitud, cancellationToken);

            _logger.LogInformation(
                "Evaluacion completada. Score: {Score}, Recomendacion: {Recomendacion}",
                resultado.ScoreFinal,
                resultado.Recomendacion);

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validacion en la solicitud");
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validacion",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la evaluacion");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrio un error al procesar la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Consulta el reporte crediticio de una persona por DNI
    /// </summary>
    [HttpGet("persona/{dni}")]
    [ProducesResponseType(typeof(ReporteCrediticioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReporteCrediticioDto>> ConsultarPersona(
        string dni,
        [FromServices] IEquifaxApiClient equifaxClient,
        CancellationToken cancellationToken)
    {
        var reporte = await equifaxClient.ConsultarReporteCrediticioAsync("1", dni, cancellationToken);

        if (reporte == null)
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = $"No se encontro informacion para el DNI: {dni}",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(reporte);
    }

    /// <summary>
    /// Consulta el reporte crediticio de una empresa por RUC
    /// </summary>
    [HttpGet("empresa/{ruc}")]
    [ProducesResponseType(typeof(ReporteCrediticioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReporteCrediticioDto>> ConsultarEmpresa(
        string ruc,
        [FromServices] IEquifaxApiClient equifaxClient,
        CancellationToken cancellationToken)
    {
        var reporte = await equifaxClient.ConsultarReporteCrediticioAsync("6", ruc, cancellationToken);

        if (reporte == null)
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = $"No se encontro informacion para el RUC: {ruc}",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(reporte);
    }
}
