using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly IReniecValidationService _reniecValidationService;
    private readonly IEquifaxApiClient _equifaxApiClient;
    private readonly ILogger<DocumentosController> _logger;

    private static readonly string[] _extensionesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
    private const long _tamanoMaximoBytes = 4 * 1024 * 1024; // 4 MB (limite F0)

    public DocumentosController(
        IDocumentIntelligenceService documentIntelligenceService,
        IReniecValidationService reniecValidationService,
        IEquifaxApiClient equifaxApiClient,
        ILogger<DocumentosController> logger)
    {
        _documentIntelligenceService = documentIntelligenceService;
        _reniecValidationService = reniecValidationService;
        _equifaxApiClient = equifaxApiClient;
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
        var validacion = ValidarArchivo(archivo);
        if (validacion != null) return validacion;

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

            // Validar DNI contra RENIEC
            if (!string.IsNullOrEmpty(resultado.NumeroDocumento))
            {
                _logger.LogInformation("Validando DNI {Dni} contra RENIEC", resultado.NumeroDocumento);
                var reniec = await _reniecValidationService.ValidarDniAsync(
                    resultado.NumeroDocumento, cancellationToken);

                resultado.DniValidado = reniec.DniValido;
                resultado.MensajeValidacion = reniec.Mensaje;
            }

            return Ok(resultado);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en Content Understanding: {Mensaje}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error al procesar documento",
                Detail = "El servicio de procesamiento de documentos no pudo analizar el archivo",
                Status = StatusCodes.Status500InternalServerError
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout en Content Understanding: {Mensaje}", ex.Message);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new ProblemDetails
            {
                Title = "Tiempo de espera agotado",
                Detail = "El servicio de procesamiento de documentos no respondio a tiempo",
                Status = StatusCodes.Status504GatewayTimeout
            });
        }
    }

    /// <summary>
    /// Procesa una Vigencia de Poder con progreso via Server-Sent Events
    /// </summary>
    [HttpPost("vigencia-poder")]
    public async Task ProcesarVigenciaPoder(
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        if (!await ValidarArchivoSseAsync(archivo, cancellationToken)) return;

        // Iniciar SSE
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation(
            "Recibido Vigencia de Poder (SSE): {NombreArchivo}, Tamano: {Tamano} bytes",
            archivo.FileName, archivo.Length);

        try
        {
            await EnviarEvento("progress", "Enviando documento a Azure Content Understanding...", cancellationToken);

            var progreso = new Progress<string>(async mensaje =>
            {
                try { await EnviarEvento("progress", mensaje, cancellationToken); }
                catch { /* SSE ya cerrado */ }
            });

            using var stream = archivo.OpenReadStream();
            var resultado = await _documentIntelligenceService.ProcesarVigenciaPoderAsync(
                stream,
                archivo.FileName,
                cancellationToken,
                progreso);

            var empresa = resultado.RazonSocial ?? "la empresa";
            await EnviarEvento("progress",
                $"Documento analizado: {empresa} (RUC: {resultado.Ruc ?? "N/A"})", cancellationToken);

            // Validar RUC contra Equifax
            if (!string.IsNullOrEmpty(resultado.Ruc))
            {
                await EnviarEvento("progress",
                    $"Validando RUC {resultado.Ruc} contra Equifax...", cancellationToken);

                try
                {
                    var reporte = await _equifaxApiClient.ConsultarReporteCrediticioAsync(
                        "RUC", resultado.Ruc, cancellationToken);

                    resultado.RucValidado = reporte != null;
                    resultado.MensajeValidacionRuc = reporte != null
                        ? "RUC verificado en Equifax"
                        : "RUC no encontrado en Equifax";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo validar RUC {Ruc} contra Equifax", resultado.Ruc);
                    resultado.RucValidado = null;
                    resultado.MensajeValidacionRuc = "No se pudo verificar el RUC";
                }

                await EnviarEvento("progress",
                    resultado.RucValidado == true
                        ? $"RUC {resultado.Ruc} verificado en Equifax"
                        : $"RUC {resultado.Ruc}: {resultado.MensajeValidacionRuc}",
                    cancellationToken);
            }

            // Validar DNIs de representantes contra RENIEC
            var totalReps = resultado.Representantes.Count(r => !string.IsNullOrEmpty(r.DocumentoIdentidad));
            var repIndex = 0;
            foreach (var rep in resultado.Representantes)
            {
                if (string.IsNullOrEmpty(rep.DocumentoIdentidad)) continue;
                repIndex++;

                await EnviarEvento("progress",
                    $"Validando representante {repIndex}/{totalReps} contra RENIEC: {rep.Nombre ?? rep.DocumentoIdentidad}...",
                    cancellationToken);

                var reniec = await _reniecValidationService.ValidarDniAsync(
                    rep.DocumentoIdentidad, cancellationToken);

                rep.DniValidado = reniec.DniValido;
                rep.MensajeValidacion = reniec.Mensaje;
            }

            // Enviar resultado final
            await EnviarEvento("progress", "Procesamiento completado", cancellationToken);
            var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await EnviarEvento("result", json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en Content Understanding: {Mensaje}", ex.Message);
            await EnviarEvento("error", "El servicio de procesamiento de documentos no pudo analizar el archivo", cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout en Content Understanding: {Mensaje}", ex.Message);
            await EnviarEvento("error", "El servicio de procesamiento de documentos no respondio a tiempo", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando Vigencia de Poder");
            await EnviarEvento("error", "Error inesperado al procesar el documento", cancellationToken);
        }
    }

    /// <summary>
    /// Procesa un Balance General con progreso via Server-Sent Events
    /// </summary>
    [HttpPost("balance-general")]
    public async Task ProcesarBalanceGeneral(
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        if (!await ValidarArchivoSseAsync(archivo, cancellationToken)) return;

        // Iniciar SSE
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation(
            "Recibido Balance General (SSE): {NombreArchivo}, Tamano: {Tamano} bytes",
            archivo.FileName, archivo.Length);

        try
        {
            await EnviarEvento("progress", "Enviando Balance General a Azure Content Understanding...", cancellationToken);

            var progreso = new Progress<string>(async mensaje =>
            {
                try { await EnviarEvento("progress", mensaje, cancellationToken); }
                catch { /* SSE ya cerrado */ }
            });

            using var stream = archivo.OpenReadStream();
            var resultado = await _documentIntelligenceService.ProcesarBalanceGeneralAsync(
                stream,
                archivo.FileName,
                cancellationToken,
                progreso);

            var empresa = resultado.RazonSocial ?? "la empresa";
            await EnviarEvento("progress",
                $"Balance procesado: {empresa} (RUC: {resultado.Ruc ?? "N/A"})", cancellationToken);

            // Validar RUC contra Equifax
            if (!string.IsNullOrEmpty(resultado.Ruc))
            {
                await EnviarEvento("progress",
                    $"Validando RUC {resultado.Ruc} contra Equifax...", cancellationToken);

                try
                {
                    var reporte = await _equifaxApiClient.ConsultarReporteCrediticioAsync(
                        "RUC", resultado.Ruc, cancellationToken);

                    resultado.RucValidado = reporte != null;
                    resultado.MensajeValidacionRuc = reporte != null
                        ? "RUC verificado en Equifax"
                        : "RUC no encontrado en Equifax";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo validar RUC {Ruc} contra Equifax", resultado.Ruc);
                    resultado.RucValidado = null;
                    resultado.MensajeValidacionRuc = "No se pudo verificar el RUC";
                }

                await EnviarEvento("progress",
                    resultado.RucValidado == true
                        ? $"RUC {resultado.Ruc} verificado en Equifax"
                        : $"RUC {resultado.Ruc}: {resultado.MensajeValidacionRuc}",
                    cancellationToken);
            }

            // Verificar cuadre contable
            await EnviarEvento("progress", "Verificando cuadre contable...", cancellationToken);
            var cuadraBalance = VerificarCuadreContable(resultado);
            if (cuadraBalance)
            {
                await EnviarEvento("progress", "✓ Balance cuadrado: Activo = Pasivo + Patrimonio", cancellationToken);
            }
            else
            {
                await EnviarEvento("progress", "⚠ Advertencia: Balance no cuadra - revisar cifras", cancellationToken);
            }

            // Validar DNIs de firmantes contra RENIEC
            var totalFirmantes = resultado.Firmantes.Count(f => !string.IsNullOrEmpty(f.Dni));
            var firmanteIndex = 0;
            foreach (var firmante in resultado.Firmantes)
            {
                if (string.IsNullOrEmpty(firmante.Dni)) continue;
                firmanteIndex++;

                await EnviarEvento("progress",
                    $"Validando firmante {firmanteIndex}/{totalFirmantes} contra RENIEC: {firmante.Nombre ?? firmante.Dni}...",
                    cancellationToken);

                var reniec = await _reniecValidationService.ValidarDniAsync(
                    firmante.Dni, cancellationToken);

                firmante.DniValidado = reniec.DniValido;
                firmante.MensajeValidacion = reniec.Mensaje;
            }

            // Enviar resultado final
            await EnviarEvento("progress", "Balance General procesado completamente", cancellationToken);
            var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await EnviarEvento("result", json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en Content Understanding: {Mensaje}", ex.Message);
            await EnviarEvento("error", "El servicio de procesamiento de documentos no pudo analizar el archivo", cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout en Content Understanding: {Mensaje}", ex.Message);
            await EnviarEvento("error", "El servicio de procesamiento de documentos no respondio a tiempo", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando Balance General");
            await EnviarEvento("error", "Error inesperado al procesar el documento", cancellationToken);
        }
    }

    /// <summary>
    /// Procesa un Estado de Resultados con progreso via Server-Sent Events
    /// </summary>
    [HttpPost("estado-resultados")]
    public async Task ProcesarEstadoResultados(
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        if (!await ValidarArchivoSseAsync(archivo, cancellationToken)) return;

        // Iniciar SSE
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation(
            "Recibido Estado de Resultados (SSE): {NombreArchivo}, Tamano: {Tamano} bytes",
            archivo.FileName, archivo.Length);

        try
        {
            using var stream = archivo.OpenReadStream();

            // Progreso para Content Understanding
            var progreso = new Progress<string>(async mensaje =>
            {
                await EnviarEvento("progress", mensaje, cancellationToken);
            });

            var resultado = await _documentIntelligenceService.ProcesarEstadoResultadosAsync(
                stream,
                archivo.FileName,
                cancellationToken,
                progreso);

            var empresa = resultado.RazonSocial ?? "la empresa";
            await EnviarEvento("progress",
                $"Estado de Resultados procesado: {empresa} (RUC: {resultado.Ruc ?? "N/A"})", cancellationToken);

            // Validar RUC contra Equifax
            if (!string.IsNullOrEmpty(resultado.Ruc))
            {
                await EnviarEvento("progress",
                    $"Validando RUC {resultado.Ruc} contra Equifax...", cancellationToken);

                try
                {
                    var reporte = await _equifaxApiClient.ConsultarReporteCrediticioAsync(
                        "RUC", resultado.Ruc, cancellationToken);

                    resultado.DatosValidosRuc = reporte != null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo validar RUC {Ruc} contra Equifax", resultado.Ruc);
                    resultado.DatosValidosRuc = false;
                }

                await EnviarEvento("progress",
                    resultado.DatosValidosRuc
                        ? $"RUC {resultado.Ruc} verificado en Equifax"
                        : $"RUC {resultado.Ruc} no se pudo verificar en Equifax",
                    cancellationToken);
            }

            // Verificar coherencia de ratios
            await EnviarEvento("progress", "Verificando coherencia de ratios financieros...", cancellationToken);
            var ratiosCoherentes = VerificarCoherenciaRatios(resultado);
            if (ratiosCoherentes)
            {
                await EnviarEvento("progress", "✓ Ratios financieros coherentes", cancellationToken);
            }
            else
            {
                await EnviarEvento("progress", "⚠ Advertencia: Algunos ratios pueden estar incorrectos", cancellationToken);
            }

            // Enviar resultado final
            await EnviarEvento("progress", "Estado de Resultados procesado completamente", cancellationToken);
            var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await EnviarEvento("result", json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en Content Understanding: {Mensaje}", ex.Message);
            await EnviarEvento("error", "El servicio de procesamiento de documentos no pudo analizar el archivo", cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout en Content Understanding: {Mensaje}", ex.Message);
            await EnviarEvento("error", "El servicio de procesamiento de documentos no respondio a tiempo", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando Estado de Resultados");
            await EnviarEvento("error", "Error inesperado al procesar el documento", cancellationToken);
        }
    }

    private static bool VerificarCoherenciaRatios(EstadoResultadosDto estado)
    {
        // Verificar que los ratios estén dentro de rangos lógicos
        if (estado.MargenBruto.HasValue && (estado.MargenBruto < -100 || estado.MargenBruto > 100))
            return false;

        if (estado.MargenOperativo.HasValue && (estado.MargenOperativo < -100 || estado.MargenOperativo > 100))
            return false;

        if (estado.MargenNeto.HasValue && (estado.MargenNeto < -100 || estado.MargenNeto > 100))
            return false;

        // Verificar progresión lógica: Bruto >= Operativo >= Neto (en términos generales)
        if (estado.MargenBruto.HasValue && estado.MargenOperativo.HasValue)
        {
            if (estado.MargenOperativo > estado.MargenBruto + 1) // +1 para tolerancia de redondeo
                return false;
        }

        return true;
    }

    private static bool VerificarCuadreContable(BalanceGeneralDto balance)
    {
        if (balance.TotalActivo == null || balance.TotalPasivoPatrimonio == null)
            return false;

        const decimal tolerancia = 0.01m; // Tolerancia de 1 centavo para redondeos
        var diferencia = Math.Abs(balance.TotalActivo.Value - balance.TotalPasivoPatrimonio.Value);
        return diferencia <= tolerancia;
    }

    private async Task EnviarEvento(string tipo, string datos, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {tipo}\ndata: {datos}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    private ActionResult? ValidarArchivo(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo requerido",
                Detail = "Debe enviar un archivo PDF o imagen",
                Status = StatusCodes.Status400BadRequest
            });
        }

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

        if (archivo.Length > _tamanoMaximoBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo muy grande",
                Detail = $"El tamano maximo permitido es {_tamanoMaximoBytes / (1024 * 1024)} MB",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return null;
    }

    private async Task<bool> ValidarArchivoSseAsync(IFormFile? archivo, CancellationToken cancellationToken)
    {
        string? error = null;

        if (archivo == null || archivo.Length == 0)
            error = "Debe enviar un archivo PDF o imagen";
        else
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!_extensionesPermitidas.Contains(extension))
                error = $"Formatos permitidos: {string.Join(", ", _extensionesPermitidas)}";
            else if (archivo.Length > _tamanoMaximoBytes)
                error = $"El tamano maximo permitido es {_tamanoMaximoBytes / (1024 * 1024)} MB";
        }

        if (error != null)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = error.Contains("Formatos") ? "Formato no soportado"
                      : error.Contains("maximo") ? "Archivo muy grande"
                      : "Archivo requerido",
                Detail = error,
                Status = 400
            }, cancellationToken);
            return false;
        }

        return true;
    }
}
