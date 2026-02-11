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

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExpedientesController(
        IExpedienteService expedienteService,
        ILogger<ExpedientesController> logger)
    {
        _expedienteService = expedienteService;
        _logger = logger;
    }

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
    /// Subir un documento al expediente (solo almacena en blob, no procesa)
    /// </summary>
    [HttpPost("{id:int}/documentos/{codigoTipo}")]
    [ProducesResponseType(typeof(DocumentoProcesadoResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentoProcesadoResumenDto>> SubirDocumento(
        int id,
        string codigoTipo,
        IFormFile archivo)
    {
        var error = ValidarArchivo(archivo);
        if (error != null)
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo invalido",
                Detail = error,
                Status = 400
            });

        _logger.LogInformation(
            "Subiendo documento {CodigoTipo} para expediente {Id}: {NombreArchivo}",
            codigoTipo, id, archivo.FileName);

        try
        {
            using var stream = archivo.OpenReadStream();
            var resultado = await _expedienteService.SubirDocumentoAsync(
                id, codigoTipo, stream, archivo.FileName);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subiendo documento {CodigoTipo} para expediente {Id}",
                codigoTipo, id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "No se pudo subir el documento",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Subir multiples documentos al expediente para clasificacion automatica
    /// </summary>
    [HttpPost("{id:int}/documentos/bulk")]
    [ProducesResponseType(typeof(List<DocumentoProcesadoResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<DocumentoProcesadoResumenDto>>> SubirDocumentosBulk(
        int id,
        [FromForm] List<IFormFile> archivos)
    {
        if (archivos == null || archivos.Count == 0)
            return BadRequest(new ProblemDetails
            {
                Title = "Solicitud invalida",
                Detail = "Debe enviar al menos un archivo",
                Status = 400
            });

        // Validar todos los archivos
        foreach (var archivo in archivos)
        {
            var error = ValidarArchivo(archivo);
            if (error != null)
                return BadRequest(new ProblemDetails
                {
                    Title = "Archivo invalido",
                    Detail = $"{archivo.FileName}: {error}",
                    Status = 400
                });
        }

        _logger.LogInformation(
            "Subiendo {Count} documentos bulk para expediente {Id}",
            archivos.Count, id);

        try
        {
            var archivosStream = new List<(Stream, string)>();
            var streams = new List<Stream>();
            try
            {
                foreach (var archivo in archivos)
                {
                    var stream = archivo.OpenReadStream();
                    streams.Add(stream);
                    archivosStream.Add((stream, archivo.FileName));
                }

                var resultado = await _expedienteService.SubirDocumentosBulkAsync(id, archivosStream);
                return Ok(resultado);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    stream.Dispose();
                }
            }
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subiendo documentos bulk para expediente {Id}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "No se pudo subir los documentos",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Reemplazar un documento existente (solo almacena en blob, no procesa)
    /// </summary>
    [HttpPut("{id:int}/documentos/{documentoId:int}")]
    [ProducesResponseType(typeof(DocumentoProcesadoResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentoProcesadoResumenDto>> ReemplazarDocumento(
        int id,
        int documentoId,
        IFormFile archivo)
    {
        var error = ValidarArchivo(archivo);
        if (error != null)
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo invalido",
                Detail = error,
                Status = 400
            });

        _logger.LogInformation(
            "Reemplazando documento {DocumentoId} en expediente {Id}: {NombreArchivo}",
            documentoId, id, archivo.FileName);

        try
        {
            using var stream = archivo.OpenReadStream();
            var resultado = await _expedienteService.ReemplazarDocumentoSubidoAsync(
                id, documentoId, stream, archivo.FileName);
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
            _logger.LogError(ex, "Error reemplazando documento {DocumentoId} en expediente {Id}", documentoId, id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error interno",
                Detail = "No se pudo reemplazar el documento",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Evaluar expediente: procesa documentos pendientes con Content Understanding y aplica reglas (SSE)
    /// </summary>
    [HttpPost("{id:int}/evaluar")]
    public async Task EvaluarExpediente(int id, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var sseGate = new SemaphoreSlim(1, 1);

        try
        {
            var progreso = new Progress<ProgresoEvaluacionDto>(async dto =>
            {
                try
                {
                    await sseGate.WaitAsync(cancellationToken);
                    try
                    {
                        var json = JsonSerializer.Serialize(dto, _jsonOptions);
                        await EnviarEvento("progress", json, cancellationToken);
                    }
                    finally
                    {
                        sseGate.Release();
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
            });

            var resultado = await _expedienteService.EvaluarExpedienteAsync(
                id, progreso, cancellationToken);

            await sseGate.WaitAsync(cancellationToken);
            try
            {
                var resultadoJson = JsonSerializer.Serialize(resultado, _jsonOptions);
                await EnviarEvento("result", resultadoJson, cancellationToken);
            }
            finally
            {
                sseGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Evaluacion del expediente {Id} cancelada por el usuario", id);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso no encontrado: {Mensaje}", ex.Message);
            await EnviarEventoSeguro("error", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de operacion: {Mensaje}", ex.Message);
            await EnviarEventoSeguro("error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluando expediente {Id}", id);
            await EnviarEventoSeguro("error", "Error inesperado al evaluar el expediente");
        }
    }

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

    private async Task EnviarEventoSeguro(string eventType, string data)
    {
        try
        {
            var mensaje = $"event: {eventType}\ndata: {data}\n\n";
            await Response.WriteAsync(mensaje);
            await Response.Body.FlushAsync();
        }
        catch { }
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
