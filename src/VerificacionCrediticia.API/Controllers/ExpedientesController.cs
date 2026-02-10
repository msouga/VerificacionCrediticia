using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpedientesController : ControllerBase
{
    private readonly IExpedienteService _expedienteService;
    private readonly ILogger<ExpedientesController> _logger;

    private static readonly string[] _extensionesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
    private const long _tamanoMaximoBytes = 4 * 1024 * 1024; // 4 MB

    public ExpedientesController(
        IExpedienteService expedienteService,
        ILogger<ExpedientesController> logger)
    {
        _expedienteService = expedienteService;
        _logger = logger;
    }

    /// <summary>
    /// Listar expedientes con paginacion
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListaExpedientesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListaExpedientesResponse>> ListarExpedientes(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1) tamanoPagina = 10;
        if (tamanoPagina > 50) tamanoPagina = 50;

        var resultado = await _expedienteService.ListarExpedientesAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }

    /// <summary>
    /// Crear un nuevo expediente crediticio
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExpedienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpedienteDto>> CrearExpediente(
        [FromBody] CrearExpedienteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var expediente = await _expedienteService.CrearExpedienteAsync(request);
            return CreatedAtAction(nameof(GetExpediente), new { id = expediente.Id }, expediente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando expediente: {Descripcion}", request.Descripcion);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "No se pudo crear el expediente",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Consultar un expediente con sus documentos
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpedienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpedienteDto>> GetExpediente(int id)
    {
        var expediente = await _expedienteService.GetExpedienteAsync(id);
        if (expediente == null)
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = $"Expediente {id} no existe",
                Status = 404
            });

        return Ok(expediente);
    }

    /// <summary>
    /// Actualizar descripcion de un expediente
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ExpedienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpedienteDto>> ActualizarExpediente(
        int id,
        [FromBody] ActualizarExpedienteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var expediente = await _expedienteService.ActualizarExpedienteAsync(id, request);
            return Ok(expediente);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = ex.Message,
                Status = 404
            });
        }
    }

    /// <summary>
    /// Eliminar expedientes en lote
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarExpedientes([FromBody] EliminarExpedientesRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new ProblemDetails
            {
                Title = "Solicitud invalida",
                Detail = "Debe especificar al menos un ID",
                Status = 400
            });

        await _expedienteService.EliminarExpedientesAsync(request.Ids);
        return NoContent();
    }

    /// <summary>
    /// Procesar un documento para un expediente (SSE con progreso)
    /// </summary>
    [HttpPost("{id:int}/documentos/{codigoTipo}")]
    public async Task ProcesarDocumento(
        int id,
        string codigoTipo,
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        // Validaciones
        var error = ValidarArchivo(archivo);
        if (error != null)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Archivo invalido",
                Detail = error,
                Status = 400
            }, cancellationToken);
            return;
        }

        // Iniciar SSE
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation(
            "Procesando documento {CodigoTipo} para expediente {Id}: {NombreArchivo}",
            codigoTipo, id, archivo.FileName);

        try
        {
            using var stream = archivo.OpenReadStream();

            var progreso = new Progress<string>(async mensaje =>
            {
                await EnviarEvento("progress", mensaje, cancellationToken);
            });

            var resultado = await _expedienteService.ProcesarDocumentoAsync(
                id, codigoTipo, stream, archivo.FileName, progreso);

            await EnviarEvento("progress", "Documento procesado correctamente", cancellationToken);

            var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await EnviarEvento("result", json, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso no encontrado: {Mensaje}", ex.Message);
            await EnviarEvento("error", ex.Message, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Tipo no soportado: {Mensaje}", ex.Message);
            await EnviarEvento("error", ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando documento {CodigoTipo} para expediente {Id}",
                codigoTipo, id);
            await EnviarEvento("error", "Error inesperado al procesar el documento", cancellationToken);
        }
    }

    /// <summary>
    /// Reemplazar un documento existente (SSE con progreso)
    /// </summary>
    [HttpPut("{id:int}/documentos/{documentoId:int}")]
    public async Task ReemplazarDocumento(
        int id,
        int documentoId,
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        var error = ValidarArchivo(archivo);
        if (error != null)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Archivo invalido",
                Detail = error,
                Status = 400
            }, cancellationToken);
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation(
            "Reemplazando documento {DocumentoId} en expediente {Id}: {NombreArchivo}",
            documentoId, id, archivo.FileName);

        try
        {
            using var stream = archivo.OpenReadStream();

            var progreso = new Progress<string>(async mensaje =>
            {
                await EnviarEvento("progress", mensaje, cancellationToken);
            });

            var resultado = await _expedienteService.ReemplazarDocumentoAsync(
                id, documentoId, stream, archivo.FileName, progreso);

            await EnviarEvento("progress", "Documento reemplazado correctamente", cancellationToken);

            var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await EnviarEvento("result", json, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            await EnviarEvento("error", ex.Message, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await EnviarEvento("error", ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reemplazando documento {DocumentoId} en expediente {Id}", documentoId, id);
            await EnviarEvento("error", "Error inesperado al reemplazar el documento", cancellationToken);
        }
    }

    /// <summary>
    /// Evaluar crediticiamente un expediente
    /// </summary>
    [HttpPost("{id:int}/evaluar")]
    [ProducesResponseType(typeof(ExpedienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpedienteDto>> EvaluarExpediente(int id)
    {
        try
        {
            var resultado = await _expedienteService.EvaluarExpedienteAsync(id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = ex.Message,
                Status = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Operacion no permitida",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando expediente {Id}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "No se pudo evaluar el expediente",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Listar tipos de documento activos
    /// </summary>
    [HttpGet("tipos-documento")]
    [ProducesResponseType(typeof(List<TipoDocumentoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TipoDocumentoDto>>> GetTiposDocumento()
    {
        var tipos = await _expedienteService.GetTiposDocumentoAsync();
        return Ok(tipos);
    }

    private async Task EnviarEvento(string eventType, string data, CancellationToken ct)
    {
        var mensaje = $"event: {eventType}\ndata: {data}\n\n";
        await Response.WriteAsync(mensaje, ct);
        await Response.Body.FlushAsync(ct);
    }

    private static string? ValidarArchivo(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return "Debe enviar un archivo PDF o imagen";

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!_extensionesPermitidas.Contains(extension))
            return $"Formatos permitidos: {string.Join(", ", _extensionesPermitidas)}";

        if (archivo.Length > _tamanoMaximoBytes)
            return $"El tamano maximo permitido es {_tamanoMaximoBytes / (1024 * 1024)} MB";

        return null;
    }
}

public class EliminarExpedientesRequest
{
    public List<int> Ids { get; set; } = new();
}
