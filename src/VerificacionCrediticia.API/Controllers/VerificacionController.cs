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
    /// Evalúa una solicitud de crédito analizando la red de relaciones empresariales
    /// </summary>
    /// <param name="solicitud">Datos del solicitante y empresa</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la evaluación con score y recomendación</returns>
    [HttpPost("evaluar")]
    [ProducesResponseType(typeof(ResultadoEvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultadoEvaluacionDto>> Evaluar(
        [FromBody] SolicitudVerificacionDto solicitud,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando evaluación para DNI: {Dni}, RUC: {Ruc}",
            solicitud.DniSolicitante,
            solicitud.RucEmpresa);

        try
        {
            var resultado = await _verificacionService.EvaluarSolicitudAsync(solicitud, cancellationToken);

            _logger.LogInformation(
                "Evaluación completada. Score: {Score}, Recomendación: {Recomendacion}",
                resultado.ScoreFinal,
                resultado.Recomendacion);

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación en la solicitud");
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la evaluación");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error al procesar la solicitud",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Consulta el estado crediticio de una persona por DNI
    /// </summary>
    [HttpGet("persona/{dni}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ConsultarPersona(
        string dni,
        [FromServices] IEquifaxApiClient equifaxClient,
        CancellationToken cancellationToken)
    {
        var persona = await equifaxClient.ConsultarPersonaAsync(dni, cancellationToken);

        if (persona == null)
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = $"No se encontró información para el DNI: {dni}",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(persona);
    }

    /// <summary>
    /// Consulta el estado crediticio de una empresa por RUC
    /// </summary>
    [HttpGet("empresa/{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ConsultarEmpresa(
        string ruc,
        [FromServices] IEquifaxApiClient equifaxClient,
        CancellationToken cancellationToken)
    {
        var empresa = await equifaxClient.ConsultarEmpresaAsync(ruc, cancellationToken);

        if (empresa == null)
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = $"No se encontró información para el RUC: {ruc}",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(empresa);
    }
}
