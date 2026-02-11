using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;
using VerificacionCrediticia.Infrastructure.ContentUnderstanding.Models;

namespace VerificacionCrediticia.Infrastructure.ContentUnderstanding;

public class ContentUnderstandingService : IDocumentIntelligenceService
{
    private readonly HttpClient _httpClient;
    private readonly ContentUnderstandingSettings _settings;
    private readonly ILogger<ContentUnderstandingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ContentUnderstandingService(
        HttpClient httpClient,
        IOptions<ContentUnderstandingSettings> settings,
        ILogger<ContentUnderstandingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.Endpoint.TrimEnd('/'));
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return;

        string detalle;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            // Azure AI Services devuelve JSON: {"error":{"code":"...","message":"..."}}
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorObj)
                && errorObj.TryGetProperty("message", out var msgProp))
            {
                var code = errorObj.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
                detalle = code != null ? $"{code}: {msgProp.GetString()}" : msgProp.GetString() ?? body;
            }
            else
            {
                detalle = body.Length > 500 ? body[..500] : body;
            }
        }
        catch
        {
            detalle = response.ReasonPhrase ?? "Sin detalle";
        }

        var mensaje = $"Content Understanding respondio {(int)response.StatusCode} ({response.StatusCode}): {detalle}";
        _logger.LogError("Error HTTP de Content Understanding: {Mensaje}", mensaje);
        throw new HttpRequestException(mensaje, null, response.StatusCode);
    }

    public async Task<DocumentoIdentidadDto> ProcesarDocumentoIdentidadAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Procesando documento con Content Understanding: {NombreArchivo}", nombreArchivo);

        // Leer el stream a bytes
        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        // POST analyzeBinary
        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.AnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        // Obtener Operation-Location para polling
        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var operationLocation = operationLocations.First();

        // Extraer el resultId de la URL
        // Formato: {endpoint}/contentunderstanding/analyzerResults/{resultId}?api-version=...
        var resultUrl = new Uri(operationLocation).PathAndQuery;

        _logger.LogInformation("Operacion iniciada, polling en: {ResultUrl}", resultUrl);

        // Polling hasta completar
        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken);

        // Mapear resultado a DTO
        var dto = MapearResultado(analyzeResult, nombreArchivo);

        _logger.LogInformation(
            "Documento procesado: DNI {Dni}, Nombre: {Nombre} {Apellido}, Sexo: {Sexo}, Confianza promedio: {Confianza:P1}",
            dto.NumeroDocumento,
            dto.Nombres,
            dto.Apellidos,
            dto.Sexo,
            dto.ConfianzaPromedio);

        return dto;
    }

    public async Task<VigenciaPoderDto> ProcesarVigenciaPoderAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        _logger.LogInformation("Procesando Vigencia de Poder con Content Understanding: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.VigenciaPoderesAnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var resultUrl = new Uri(operationLocations.First()).PathAndQuery;
        _logger.LogInformation("Operacion Vigencia de Poder iniciada, polling en: {ResultUrl}", resultUrl);

        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken, progreso);
        var dto = MapearResultadoVigenciaPoder(analyzeResult, nombreArchivo);

        _logger.LogInformation(
            "Vigencia de Poder procesada: RUC {Ruc}, Empresa: {Empresa}, Representantes: {Count}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.Representantes.Count, dto.ConfianzaPromedio);

        return dto;
    }

    private VigenciaPoderDto MapearResultadoVigenciaPoder(AnalyzeResultResponse analyzeResult, string nombreArchivo)
    {
        var dto = new VigenciaPoderDto { ArchivoOrigen = nombreArchivo };

        var contentItem = analyzeResult.Result?.Contents?.FirstOrDefault();
        if (contentItem?.Fields == null)
        {
            _logger.LogWarning("No se encontraron campos en el resultado de Vigencia de Poder");
            return dto;
        }

        var fields = contentItem.Fields;

        dto.Ruc = LimpiarRuc(ObtenerValorString(fields, "Ruc"));
        dto.RazonSocial = ObtenerValorString(fields, "RazonSocial");
        dto.TipoPersonaJuridica = ObtenerValorString(fields, "TipoPersonaJuridica");
        dto.Domicilio = ObtenerValorString(fields, "Domicilio");
        dto.ObjetoSocial = ObtenerValorString(fields, "ObjetoSocial");
        dto.CapitalSocial = ObtenerValorString(fields, "CapitalSocial");
        dto.PartidaRegistral = ObtenerValorString(fields, "PartidaRegistral");
        dto.FechaConstitucion = ObtenerValorString(fields, "FechaConstitucion")
                                ?? ObtenerValorDate(fields, "FechaConstitucion");

        // Extraer representantes del array
        if (fields.TryGetValue("Representantes", out var repField) && repField.ValueArray != null)
        {
            foreach (var item in repField.ValueArray)
            {
                if (item.ValueObject == null) continue;

                var rep = new RepresentanteDto
                {
                    Nombre = ObtenerValorString(item.ValueObject, "Nombre"),
                    DocumentoIdentidad = LimpiarNumeroDocumentoPeru(
                        ObtenerValorString(item.ValueObject, "DocumentoIdentidad")),
                    Cargo = ObtenerValorString(item.ValueObject, "Cargo"),
                    FechaNombramiento = ObtenerValorString(item.ValueObject, "FechaNombramiento")
                                        ?? ObtenerValorDate(item.ValueObject, "FechaNombramiento"),
                    Facultades = ObtenerValorString(item.ValueObject, "Facultades")
                };
                dto.Representantes.Add(rep);
            }
        }

        // Registrar confianza por campo (solo campos de empresa, no el array)
        foreach (var (nombre, field) in fields)
        {
            if (field.Confidence.HasValue && nombre != "Representantes")
            {
                dto.Confianza[nombre] = field.Confidence.Value;
            }
        }

        if (dto.Confianza.Count > 0)
        {
            dto.ConfianzaPromedio = dto.Confianza.Values.Average();
        }

        return dto;
    }

    public async Task<BalanceGeneralDto> ProcesarBalanceGeneralAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        _logger.LogInformation("Procesando Balance General con Content Understanding: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.BalanceGeneralAnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var resultUrl = new Uri(operationLocations.First()).PathAndQuery;
        _logger.LogInformation("Operacion Balance General iniciada, polling en: {ResultUrl}", resultUrl);

        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken, progreso);
        var dto = MapearResultadoBalanceGeneral(analyzeResult, nombreArchivo);

        _logger.LogInformation(
            "Balance General procesado: RUC {Ruc}, Empresa: {Empresa}, Total Activo: {TotalActivo}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.TotalActivo, dto.ConfianzaPromedio);

        return dto;
    }

    private BalanceGeneralDto MapearResultadoBalanceGeneral(AnalyzeResultResponse analyzeResult, string nombreArchivo)
    {
        var dto = new BalanceGeneralDto { ArchivoOrigen = nombreArchivo };

        var contentItem = analyzeResult.Result?.Contents?.FirstOrDefault();
        if (contentItem?.Fields == null)
        {
            _logger.LogWarning("No se encontraron campos en el resultado de Balance General");
            return dto;
        }

        var fields = contentItem.Fields;

        // Mapear encabezado
        dto.Ruc = LimpiarRuc(ObtenerValorString(fields, "Ruc"));
        dto.RazonSocial = ObtenerValorString(fields, "RazonSocial");
        dto.Domicilio = ObtenerValorString(fields, "Domicilio");
        dto.FechaBalance = ObtenerValorString(fields, "FechaBalance") ?? ObtenerValorDate(fields, "FechaBalance");
        dto.Moneda = ObtenerValorString(fields, "Moneda");

        // Mapear activos corrientes
        dto.EfectivoEquivalentes = ObtenerValorDecimal(fields, "EfectivoEquivalentes");
        dto.CuentasCobrarComerciales = ObtenerValorDecimal(fields, "CuentasCobrarComerciales");
        dto.CuentasCobrarDiversas = ObtenerValorDecimal(fields, "CuentasCobrarDiversas");
        dto.Existencias = ObtenerValorDecimal(fields, "Existencias");
        dto.GastosPagadosAnticipado = ObtenerValorDecimal(fields, "GastosPagadosAnticipado");
        dto.TotalActivoCorriente = ObtenerValorDecimal(fields, "TotalActivoCorriente");

        // Mapear activos no corrientes
        dto.InmueblesMaquinariaEquipo = ObtenerValorDecimal(fields, "InmueblesMaquinariaEquipo");
        dto.DepreciacionAcumulada = ObtenerValorDecimal(fields, "DepreciacionAcumulada");
        dto.Intangibles = ObtenerValorDecimal(fields, "Intangibles");
        dto.AmortizacionAcumulada = ObtenerValorDecimal(fields, "AmortizacionAcumulada");
        dto.ActivoDiferido = ObtenerValorDecimal(fields, "ActivoDiferido");
        dto.TotalActivoNoCorriente = ObtenerValorDecimal(fields, "TotalActivoNoCorriente");

        // Total activo
        dto.TotalActivo = ObtenerValorDecimal(fields, "TotalActivo");

        // Mapear pasivos corrientes
        dto.TributosPorPagar = ObtenerValorDecimal(fields, "TributosPorPagar");
        dto.RemuneracionesPorPagar = ObtenerValorDecimal(fields, "RemuneracionesPorPagar");
        dto.CuentasPagarComerciales = ObtenerValorDecimal(fields, "CuentasPagarComerciales");
        dto.ObligacionesFinancierasCorto = ObtenerValorDecimal(fields, "ObligacionesFinancierasCorto");
        dto.OtrasCuentasPorPagar = ObtenerValorDecimal(fields, "OtrasCuentasPorPagar");
        dto.TotalPasivoCorriente = ObtenerValorDecimal(fields, "TotalPasivoCorriente");

        // Mapear pasivos no corrientes
        dto.ObligacionesFinancierasLargo = ObtenerValorDecimal(fields, "ObligacionesFinancierasLargo");
        dto.Provisiones = ObtenerValorDecimal(fields, "Provisiones");
        dto.TotalPasivoNoCorriente = ObtenerValorDecimal(fields, "TotalPasivoNoCorriente");

        // Total pasivo
        dto.TotalPasivo = ObtenerValorDecimal(fields, "TotalPasivo");

        // Mapear patrimonio
        dto.CapitalSocial = ObtenerValorDecimal(fields, "CapitalSocial");
        dto.ReservaLegal = ObtenerValorDecimal(fields, "ReservaLegal");
        dto.ResultadosAcumulados = ObtenerValorDecimal(fields, "ResultadosAcumulados");
        dto.ResultadoEjercicio = ObtenerValorDecimal(fields, "ResultadoEjercicio");
        dto.TotalPatrimonio = ObtenerValorDecimal(fields, "TotalPatrimonio");

        // Total pasivo + patrimonio
        dto.TotalPasivoPatrimonio = ObtenerValorDecimal(fields, "TotalPasivoPatrimonio");

        // Extraer firmantes del array
        if (fields.TryGetValue("Firmantes", out var firmantesField) && firmantesField.ValueArray != null)
        {
            foreach (var item in firmantesField.ValueArray)
            {
                if (item.ValueObject == null) continue;

                var firmante = new FirmanteDto
                {
                    Nombre = ObtenerValorString(item.ValueObject, "Nombre"),
                    Dni = LimpiarNumeroDocumentoPeru(ObtenerValorString(item.ValueObject, "Dni")),
                    Cargo = ObtenerValorString(item.ValueObject, "Cargo"),
                    Matricula = ObtenerValorString(item.ValueObject, "Matricula")
                };
                dto.Firmantes.Add(firmante);
            }
        }

        // Registrar confianza por campo (excluir el array)
        foreach (var (nombre, field) in fields)
        {
            if (field.Confidence.HasValue && nombre != "Firmantes")
            {
                dto.Confianza[nombre] = field.Confidence.Value;
            }
        }

        if (dto.Confianza.Count > 0)
        {
            dto.ConfianzaPromedio = dto.Confianza.Values.Average();
        }

        // Calcular ratios financieros
        CalcularRatiosFinancieros(dto);

        return dto;
    }

    private static void CalcularRatiosFinancieros(BalanceGeneralDto balance)
    {
        // Ratio de Liquidez = Activo Corriente / Pasivo Corriente
        if (balance.TotalActivoCorriente > 0 && balance.TotalPasivoCorriente > 0)
        {
            balance.RatioLiquidez = Math.Round(
                balance.TotalActivoCorriente.Value / balance.TotalPasivoCorriente.Value, 2);
        }

        // Ratio de Endeudamiento = Total Pasivo / Total Activo
        if (balance.TotalPasivo > 0 && balance.TotalActivo > 0)
        {
            balance.RatioEndeudamiento = Math.Round(
                balance.TotalPasivo.Value / balance.TotalActivo.Value, 2);
        }

        // Ratio de Solvencia = Total Patrimonio / Total Activo
        if (balance.TotalPatrimonio > 0 && balance.TotalActivo > 0)
        {
            balance.RatioSolvencia = Math.Round(
                balance.TotalPatrimonio.Value / balance.TotalActivo.Value, 2);
        }

        // Capital de Trabajo = Activo Corriente - Pasivo Corriente
        if (balance.TotalActivoCorriente > 0 && balance.TotalPasivoCorriente > 0)
        {
            balance.CapitalTrabajo = Math.Round(
                balance.TotalActivoCorriente.Value - balance.TotalPasivoCorriente.Value, 2);
        }
    }

    private static decimal? ObtenerValorDecimal(Dictionary<string, AnalyzeField> fields, string nombre)
    {
        if (!fields.TryGetValue(nombre, out var field))
            return null;

        // Intentar parsear como string
        if (!string.IsNullOrEmpty(field.ValueString))
        {
            // Limpiar formato de numero (quitar comas, simbolos de moneda, etc.)
            var valorLimpio = field.ValueString
                .Replace(",", "")
                .Replace("S/", "")
                .Replace("$", "")
                .Replace("(", "-")
                .Replace(")", "")
                .Trim();

            if (decimal.TryParse(valorLimpio, out var valor))
                return valor;
        }

        return null;
    }

    private static string? LimpiarRuc(string? ruc)
    {
        if (string.IsNullOrEmpty(ruc))
            return ruc;

        var soloDigitos = Regex.Replace(ruc, @"[^\d]", "");
        return soloDigitos.Length == 11 ? soloDigitos : soloDigitos;
    }

    private async Task<AnalyzeResultResponse> PollForResultAsync(
        string resultUrl,
        CancellationToken cancellationToken,
        IProgress<string>? progreso = null)
    {
        var timeout = TimeSpan.FromMilliseconds(_settings.PollingTimeoutMs);
        var interval = TimeSpan.FromMilliseconds(_settings.PollingIntervalMs);
        var deadline = DateTime.UtcNow + timeout;
        var inicio = DateTime.UtcNow;
        var pollCount = 0;

        string[] mensajes =
        [
            "Analizando estructura del documento...",
            "Extrayendo texto con OCR...",
            "Identificando campos del documento...",
            "Procesando con inteligencia artificial...",
            "Validando datos extraidos...",
            "Finalizando analisis...",
            "El documento es extenso, esto puede tomar un poco mas...",
            "Aun procesando, por favor espere...",
            "Casi listo..."
        ];

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(interval, cancellationToken);
            pollCount++;

            var elapsed = (int)(DateTime.UtcNow - inicio).TotalSeconds;
            var mensajeIndex = Math.Min(elapsed / 10, mensajes.Length - 1);
            progreso?.Report($"{mensajes[mensajeIndex]} ({elapsed}s)");

            var response = await _httpClient.GetAsync(resultUrl, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<AnalyzeResultResponse>(json, JsonOptions);

            if (result == null)
                throw new InvalidOperationException("Respuesta de polling deserializada como null");

            _logger.LogDebug("Polling status: {Status} ({Elapsed}s, intento {Count})", result.Status, elapsed, pollCount);

            if (result.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase))
                return result;

            if (result.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"El analisis fallo. ID: {result.Id}");
        }

        throw new TimeoutException(
            $"Content Understanding no completo el analisis en {_settings.PollingTimeoutMs}ms");
    }

    private DocumentoIdentidadDto MapearResultado(AnalyzeResultResponse analyzeResult, string nombreArchivo)
    {
        var dto = new DocumentoIdentidadDto
        {
            ArchivoOrigen = nombreArchivo,
            TipoDocumento = "DNI"
        };

        var contentItem = analyzeResult.Result?.Contents?.FirstOrDefault();
        if (contentItem?.Fields == null)
        {
            _logger.LogWarning("No se encontraron campos en el resultado del analisis");
            return dto;
        }

        var fields = contentItem.Fields;

        dto.Nombres = ObtenerValorString(fields, "Nombres");
        dto.Apellidos = ObtenerValorString(fields, "Apellidos");
        dto.NumeroDocumento = LimpiarNumeroDocumentoPeru(ObtenerValorString(fields, "NumeroDocumento"));
        dto.FechaNacimiento = ObtenerValorString(fields, "FechaNacimiento")
                              ?? ObtenerValorDate(fields, "FechaNacimiento");
        dto.FechaExpiracion = ObtenerValorString(fields, "FechaExpiracion")
                              ?? ObtenerValorDate(fields, "FechaExpiracion");
        dto.Sexo = ObtenerValorString(fields, "Sexo");
        dto.EstadoCivil = ObtenerValorString(fields, "EstadoCivil");
        dto.Direccion = ObtenerValorString(fields, "Direccion");

        // Nacionalidad hardcodeada - DNI peruano siempre es PER
        dto.Nacionalidad = "PER";

        // Registrar confianza por campo
        foreach (var (nombre, field) in fields)
        {
            if (field.Confidence.HasValue)
            {
                dto.Confianza[nombre] = field.Confidence.Value;
            }
        }

        if (dto.Confianza.Count > 0)
        {
            dto.ConfianzaPromedio = dto.Confianza.Values.Average();
        }

        return dto;
    }

    private static string? ObtenerValorString(Dictionary<string, AnalyzeField> fields, string nombre)
    {
        if (fields.TryGetValue(nombre, out var field) && !string.IsNullOrEmpty(field.ValueString))
            return field.ValueString;

        return null;
    }

    private static string? ObtenerValorDate(Dictionary<string, AnalyzeField> fields, string nombre)
    {
        if (fields.TryGetValue(nombre, out var field) && !string.IsNullOrEmpty(field.ValueDate))
        {
            // Intentar convertir de YYYY-MM-DD a DD/MM/YYYY
            if (DateOnly.TryParse(field.ValueDate, out var date))
                return date.ToString("dd/MM/yyyy");

            return field.ValueDate;
        }

        return null;
    }

    private static string? LimpiarNumeroDocumentoPeru(string? numero)
    {
        if (string.IsNullOrEmpty(numero))
            return numero;

        var soloDigitos = Regex.Replace(numero, @"[^\d]", "");

        // DNI peruano tiene 8 digitos; si hay 9, el ultimo es digito verificador
        if (soloDigitos.Length >= 8)
            return soloDigitos[..8];

        return soloDigitos;
    }

    public async Task<FichaRucDto> ProcesarFichaRucAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        _logger.LogInformation("Procesando Ficha RUC con Content Understanding: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.FichaRucAnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var resultUrl = new Uri(operationLocations.First()).PathAndQuery;
        _logger.LogInformation("Operacion Ficha RUC iniciada, polling en: {ResultUrl}", resultUrl);

        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken, progreso);
        var dto = MapearResultadoFichaRuc(analyzeResult, nombreArchivo);

        _logger.LogInformation(
            "Ficha RUC procesada: RUC {Ruc}, Empresa: {Empresa}, Estado: {Estado}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.EstadoContribuyente, dto.ConfianzaPromedio);

        return dto;
    }

    private FichaRucDto MapearResultadoFichaRuc(AnalyzeResultResponse analyzeResult, string nombreArchivo)
    {
        var dto = new FichaRucDto { ArchivoOrigen = nombreArchivo };

        var contentItem = analyzeResult.Result?.Contents?.FirstOrDefault();
        if (contentItem?.Fields == null)
        {
            _logger.LogWarning("No se encontraron campos en el resultado de Ficha RUC");
            return dto;
        }

        var fields = contentItem.Fields;

        dto.Ruc = LimpiarRuc(ObtenerValorString(fields, "Ruc"));
        dto.RazonSocial = ObtenerValorString(fields, "RazonSocial");
        dto.NombreComercial = ObtenerValorString(fields, "NombreComercial");
        dto.TipoContribuyente = ObtenerValorString(fields, "TipoContribuyente");
        dto.FechaInscripcion = ObtenerValorString(fields, "FechaInscripcion")
                                ?? ObtenerValorDate(fields, "FechaInscripcion");
        dto.FechaInicioActividades = ObtenerValorString(fields, "FechaInicioActividades")
                                      ?? ObtenerValorDate(fields, "FechaInicioActividades");
        dto.EstadoContribuyente = ObtenerValorString(fields, "EstadoContribuyente");
        dto.CondicionDomicilio = ObtenerValorString(fields, "CondicionDomicilio");
        dto.DomicilioFiscal = ObtenerValorString(fields, "DomicilioFiscal");
        dto.ActividadEconomica = ObtenerValorString(fields, "ActividadEconomica");
        dto.SistemaContabilidad = ObtenerValorString(fields, "SistemaContabilidad");
        dto.ComprobantesAutorizados = ObtenerValorString(fields, "ComprobantesAutorizados");

        foreach (var (nombre, field) in fields)
        {
            if (field.Confidence.HasValue)
            {
                dto.Confianza[nombre] = field.Confidence.Value;
            }
        }

        if (dto.Confianza.Count > 0)
        {
            dto.ConfianzaPromedio = dto.Confianza.Values.Average();
        }

        return dto;
    }

    public async Task<ClasificacionResultadoDto> ClasificarYProcesarAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        _logger.LogInformation("Clasificando documento con Content Understanding: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.ClasificadorAnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var resultUrl = new Uri(operationLocations.First()).PathAndQuery;
        _logger.LogInformation("Clasificacion iniciada, polling en: {ResultUrl}", resultUrl);

        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken, progreso);

        // La respuesta tiene multiples contents: el principal (sin category) y el segmento clasificado (con category y fields)
        var contentItem = analyzeResult.Result?.Contents?
            .FirstOrDefault(c => !string.IsNullOrEmpty(c.Category));
        var categoria = contentItem?.Category ?? "other";

        _logger.LogInformation("Documento clasificado como: {Categoria}", categoria);

        // Para mapear, necesitamos un AnalyzeResultResponse con el content correcto en primera posicion
        // ya que los metodos de mapeo usan Contents.FirstOrDefault()
        var resultadoParaMapeo = new AnalyzeResultResponse
        {
            Id = analyzeResult.Id,
            Status = analyzeResult.Status,
            Result = new AnalyzeResult
            {
                AnalyzerId = analyzeResult.Result?.AnalyzerId ?? string.Empty,
                Contents = contentItem != null ? new List<AnalyzeContent> { contentItem } : new List<AnalyzeContent>()
            }
        };

        // Mapear resultado de extraccion segun la categoria detectada
        object? resultadoExtraccion = categoria switch
        {
            "DNI" => MapearResultado(resultadoParaMapeo, nombreArchivo),
            "VIGENCIA_PODER" => MapearResultadoVigenciaPoder(resultadoParaMapeo, nombreArchivo),
            "BALANCE_GENERAL" => MapearResultadoBalanceGeneral(resultadoParaMapeo, nombreArchivo),
            "ESTADO_RESULTADOS" => MapearResultadoEstadoResultados(resultadoParaMapeo, nombreArchivo),
            "FICHA_RUC" => MapearResultadoFichaRuc(resultadoParaMapeo, nombreArchivo),
            _ => null
        };

        // Calcular confianza de clasificacion como promedio de confianza de campos extraidos
        decimal confianzaClasificacion = 0;
        if (contentItem?.Fields != null)
        {
            var confidenceValues = contentItem.Fields
                .Where(f => f.Value?.Confidence != null)
                .Select(f => (decimal)f.Value.Confidence!.Value)
                .ToList();
            if (confidenceValues.Count > 0)
                confianzaClasificacion = confidenceValues.Average();
        }

        return new ClasificacionResultadoDto
        {
            CategoriaDetectada = categoria,
            ResultadoExtraccion = resultadoExtraccion,
            ConfianzaClasificacion = confianzaClasificacion
        };
    }

    public async Task<EstadoResultadosDto> ProcesarEstadoResultadosAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        _logger.LogInformation("Procesando Estado de Resultados con Content Understanding: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.EstadoResultadosAnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var resultUrl = new Uri(operationLocations.First()).PathAndQuery;
        _logger.LogInformation("Operacion Estado de Resultados iniciada, polling en: {ResultUrl}", resultUrl);

        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken, progreso);
        var dto = MapearResultadoEstadoResultados(analyzeResult, nombreArchivo);

        _logger.LogInformation(
            "Estado de Resultados procesado: RUC {Ruc}, Empresa: {Empresa}, Ventas: {Ventas}, Utilidad Neta: {UtilidadNeta}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.VentasNetas, dto.UtilidadNeta, dto.ConfianzaPromedio);

        return dto;
    }

    private EstadoResultadosDto MapearResultadoEstadoResultados(AnalyzeResultResponse analyzeResult, string nombreArchivo)
    {
        var dto = new EstadoResultadosDto
        {
            FechaProcesado = DateTime.UtcNow
        };

        var contentItem = analyzeResult.Result?.Contents?.FirstOrDefault();
        if (contentItem?.Fields == null)
        {
            _logger.LogWarning("No se encontraron campos en el resultado de Estado de Resultados");
            return dto;
        }

        var fields = contentItem.Fields;

        // Mapear encabezado
        dto.Ruc = LimpiarRuc(ObtenerValorString(fields, "Ruc"));
        dto.RazonSocial = ObtenerValorString(fields, "RazonSocial");
        dto.Periodo = ObtenerValorString(fields, "Periodo");
        dto.Moneda = ObtenerValorString(fields, "Moneda");

        // Mapear partidas financieras
        dto.VentasNetas = ObtenerValorDecimal(fields, "VentasNetas");
        dto.CostoVentas = ObtenerValorDecimal(fields, "CostoVentas");
        dto.UtilidadBruta = ObtenerValorDecimal(fields, "UtilidadBruta");
        dto.GastosAdministrativos = ObtenerValorDecimal(fields, "GastosAdministrativos");
        dto.GastosVentas = ObtenerValorDecimal(fields, "GastosVentas");
        dto.UtilidadOperativa = ObtenerValorDecimal(fields, "UtilidadOperativa");
        dto.OtrosIngresos = ObtenerValorDecimal(fields, "OtrosIngresos");
        dto.OtrosGastos = ObtenerValorDecimal(fields, "OtrosGastos");
        dto.UtilidadAntesImpuestos = ObtenerValorDecimal(fields, "UtilidadAntesImpuestos");
        dto.ImpuestoRenta = ObtenerValorDecimal(fields, "ImpuestoRenta");
        dto.UtilidadNeta = ObtenerValorDecimal(fields, "UtilidadNeta");

        // Calcular ratios
        dto.CalcularRatios();

        // Validar RUC
        dto.DatosValidosRuc = !string.IsNullOrEmpty(dto.Ruc) && dto.Ruc.Length == 11;

        // Calcular confianza promedio
        var confidenceValues = fields
            .Where(f => f.Value?.Confidence != null)
            .Select(f => f.Value.Confidence!.Value)
            .ToList();

        if (confidenceValues.Count > 0)
        {
            dto.ConfianzaPromedio = (decimal)confidenceValues.Average();
        }

        _logger.LogInformation("Estado de Resultados mapeado: {CamposExtraidos} campos, confianza promedio: {Confianza:P1}",
            fields.Count, dto.ConfianzaPromedio);

        return dto;
    }
}
