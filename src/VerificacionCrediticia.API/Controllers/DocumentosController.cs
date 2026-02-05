using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly ILogger<DocumentosController> _logger;

    private static readonly string[] _extensionesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
    private const long _tamanoMaximoBytes = 4 * 1024 * 1024; // 4 MB (limite F0)

    public DocumentosController(
        IDocumentIntelligenceService documentIntelligenceService,
        ILogger<DocumentosController> logger)
    {
        _documentIntelligenceService = documentIntelligenceService;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un documento de identidad (DNI) y extrae sus datos usando Azure AI Document Intelligence
    /// </summary>
    [HttpPost("dni")]
    [ProducesResponseType(typeof(DocumentoIdentidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentoIdentidadDto>> ProcesarDni(
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        // Validar que se envio un archivo
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo requerido",
                Detail = "Debe enviar un archivo PDF o imagen del DNI",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Validar extension
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!_extensionesPermitidas.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Formato no soportado",
                Detail = $"Formatos permitidos: {string.Join(", ", _extensionesPermitidas)}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Validar tamano
        if (archivo.Length > _tamanoMaximoBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo muy grande",
                Detail = $"El tamano maximo permitido es {_tamanoMaximoBytes / (1024 * 1024)} MB",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation(
            "Recibido documento DNI: {NombreArchivo}, Tamano: {Tamano} bytes",
            archivo.FileName,
            archivo.Length);

        try
        {
            using var stream = archivo.OpenReadStream();
            var resultado = await _documentIntelligenceService.ProcesarDocumentoIdentidadAsync(
                stream,
                archivo.FileName,
                cancellationToken);

            return Ok(resultado);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Error en Azure Document Intelligence: {Mensaje}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error al procesar documento",
                Detail = "El servicio de procesamiento de documentos no pudo analizar el archivo",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
