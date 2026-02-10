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
        response.EnsureSuccessStatusCode();

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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Procesando Vigencia de Poder con Content Understanding: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var analyzeUrl = $"/contentunderstanding/analyzers/{_settings.VigenciaPoderesAnalyzerId}:analyzeBinary?api-version={_settings.ApiVersion}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PostAsync(analyzeUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
        {
            throw new InvalidOperationException("La respuesta no contiene el header Operation-Location");
        }

        var resultUrl = new Uri(operationLocations.First()).PathAndQuery;
        _logger.LogInformation("Operacion Vigencia de Poder iniciada, polling en: {ResultUrl}", resultUrl);

        var analyzeResult = await PollForResultAsync(resultUrl, cancellationToken);
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

    private static string? LimpiarRuc(string? ruc)
    {
        if (string.IsNullOrEmpty(ruc))
            return ruc;

        var soloDigitos = Regex.Replace(ruc, @"[^\d]", "");
        return soloDigitos.Length == 11 ? soloDigitos : soloDigitos;
    }

    private async Task<AnalyzeResultResponse> PollForResultAsync(
        string resultUrl,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(_settings.PollingTimeoutMs);
        var interval = TimeSpan.FromMilliseconds(_settings.PollingIntervalMs);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(interval, cancellationToken);

            var response = await _httpClient.GetAsync(resultUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<AnalyzeResultResponse>(json, JsonOptions);

            if (result == null)
                throw new InvalidOperationException("Respuesta de polling deserializada como null");

            _logger.LogDebug("Polling status: {Status}", result.Status);

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
}
