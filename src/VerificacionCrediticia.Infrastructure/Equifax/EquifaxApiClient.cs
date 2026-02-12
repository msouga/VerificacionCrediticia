using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;
using VerificacionCrediticia.Core.Services;
using VerificacionCrediticia.Infrastructure.Equifax.Models;

namespace VerificacionCrediticia.Infrastructure.Equifax;

public class EquifaxApiClient : IEquifaxApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IEquifaxAuthService _authService;
    private readonly EquifaxSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EquifaxApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EquifaxApiClient(
        HttpClient httpClient,
        IEquifaxAuthService authService,
        IOptions<EquifaxSettings> settings,
        IMemoryCache cache,
        ILogger<EquifaxApiClient> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ReporteCrediticioDto?> ConsultarReporteCrediticioAsync(
        string tipoDocumento,
        string numeroDocumento,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"reporte_{tipoDocumento}_{numeroDocumento}";
        if (_cache.TryGetValue(cacheKey, out ReporteCrediticioDto? cached))
        {
            return cached;
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        var url = $"{_settings.EffectiveBaseUrl}/datos-comerciales/transaction/execute";

        var tipoPersona = tipoDocumento == "6" ? "2" : "1";
        var requestBody = new
        {
            applicants = new
            {
                primaryConsumer = new
                {
                    personalInformation = new
                    {
                        id = numeroDocumento,
                        tipoPersona,
                        tipoDocumento
                    }
                }
            },
            productData = new
            {
                billTo = _settings.BillTo ?? "",
                shipTo = _settings.ShipTo ?? "",
                productName = "PEREPORT",
                productOrch = "CREDITREPORTV2",
                configuration = "Config",
                customer = "PEREPRSM",
                model = "CreditReportRSM"
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // === DEBUG: Capturar request completo ===
        _logger.LogDebug("=== EQUIFAX REQUEST ===");
        _logger.LogDebug("URL: POST {Url}", url);
        _logger.LogDebug("Request Headers:");
        foreach (var h in request.Headers)
            _logger.LogDebug("  {Key}: {Value}", h.Key, string.Join(", ", h.Value));
        if (request.Content != null)
        {
            foreach (var h in request.Content.Headers)
                _logger.LogDebug("  {Key}: {Value}", h.Key, string.Join(", ", h.Value));
        }
        _logger.LogDebug("Request Body:\n{Body}", jsonBody);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        // === DEBUG: Capturar response completo ===
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("=== EQUIFAX RESPONSE ===");
        _logger.LogDebug("Status: {StatusCode} ({StatusInt})", response.StatusCode, (int)response.StatusCode);
        _logger.LogDebug("Response Headers:");
        foreach (var h in response.Headers)
            _logger.LogDebug("  {Key}: {Value}", h.Key, string.Join(", ", h.Value));
        foreach (var h in response.Content.Headers)
            _logger.LogDebug("  {Key}: {Value}", h.Key, string.Join(", ", h.Value));
        _logger.LogDebug("Response Body ({Length} chars):\n{Body}",
            responseBody.Length, responseBody.Length > 10000 ? responseBody[..10000] + "..." : responseBody);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = responseBody;
        var equifaxResponse = JsonSerializer.Deserialize<EquifaxCreditReportResponse>(content, _jsonOptions);

        if (equifaxResponse?.Applicants?.PrimaryConsumer?.InterconnectResponse == null)
        {
            _logger.LogDebug(
                "Equifax respuesta parseada sin InterconnectResponse. Applicants={A}, PrimaryConsumer={PC}",
                equifaxResponse?.Applicants != null,
                equifaxResponse?.Applicants?.PrimaryConsumer != null);
            return null;
        }

        var reporte = MapearReporte(equifaxResponse, tipoDocumento, numeroDocumento);

        _cache.Set(cacheKey, reporte, TimeSpan.FromMinutes(_settings.CacheMinutes));

        return reporte;
    }

    private ReporteCrediticioDto MapearReporte(
        EquifaxCreditReportResponse response,
        string tipoDocumento,
        string numeroDocumento)
    {
        var reporte = new ReporteCrediticioDto
        {
            TipoDocumento = tipoDocumento,
            NumeroDocumento = numeroDocumento
        };

        var modulos = response.Applicants!.PrimaryConsumer!.InterconnectResponse!;

        // Verificar si el modulo 100 (Resumen) tiene error (documento no encontrado)
        var moduloResumen = modulos.FirstOrDefault(m => m.Codigo == 100);
        if (moduloResumen?.TieneError == true)
        {
            return reporte;
        }

        foreach (var modulo in modulos)
        {
            if (modulo.TieneError)
                continue;

            if (modulo.Data == null || !modulo.Data.Flag)
                continue;

            switch (modulo.Codigo)
            {
                case 602: // Directorio de Personas
                    MapearDirectorioPersona(reporte, modulo.Data);
                    break;

                case 877 or 878: // Directorio SUNAT
                    MapearDirectorioSunat(reporte, modulo.Data);
                    break;

                case 855 or 856: // Representantes Legales
                    MapearRepresentantesLegales(reporte, modulo.Data);
                    break;

                case 875 or 876: // Empresas Relacionadas
                    MapearEmpresasRelacionadas(reporte, modulo.Data);
                    break;

                case 865 or 868: // Score / NivelRiesgo principal del consultado
                    MapearNivelRiesgoPrincipal(reporte, modulo.Data);
                    break;
            }
        }

        return reporte;
    }

    private void MapearDirectorioPersona(ReporteCrediticioDto reporte, EquifaxModuloData data)
    {
        var dir = data.DirectorioPersona;
        if (dir == null) return;

        var nombreCompleto = dir.Nombres ?? $"{dir.PrimerNombre} {dir.SegundoNombre} {dir.ApellidoPaterno} {dir.ApellidoMaterno}".Trim();

        reporte.DatosPersona = new DatosPersonaDto
        {
            Nombres = nombreCompleto,
            FechaNacimiento = dir.FechaNacimiento,
            EstadoCivil = dir.EstadoCivil,
            Nacionalidad = dir.Nacionalidad
        };
    }

    private void MapearDirectorioSunat(ReporteCrediticioDto reporte, EquifaxModuloData data)
    {
        var sunat = data.DirectorioSUNAT?.Directorio?.FirstOrDefault();
        if (sunat == null) return;

        reporte.DatosEmpresa = new DatosEmpresaDto
        {
            RazonSocial = sunat.RazonSocial ?? string.Empty,
            NombreComercial = sunat.NombreComercial,
            TipoContribuyente = sunat.TipoContribuyente,
            EstadoContribuyente = sunat.EstadoContribuyente,
            CondicionContribuyente = sunat.CondicionContribuyente,
            InicioActividades = sunat.InicioActividades
        };
    }

    private void MapearRepresentantesLegales(ReporteCrediticioDto reporte, EquifaxModuloData data)
    {
        var reps = data.RepresentantesLegales;
        if (reps == null) return;

        if (reps.RepresentadoPor?.RepresentadoPor != null)
        {
            reporte.RepresentadoPor = reps.RepresentadoPor.RepresentadoPor
                .Select(MapearRepresentanteLegal)
                .ToList();
        }

        if (reps.RepresentantesDe?.RepresentantesDe != null)
        {
            reporte.RepresentantesDe = reps.RepresentantesDe.RepresentantesDe
                .Select(MapearRepresentanteLegal)
                .ToList();
        }
    }

    private RepresentanteLegalDto MapearRepresentanteLegal(EquifaxRepresentanteLegal rep)
    {
        var riesgoTexto = rep.ScoreHistoricos?.ScoreActual?.Riesgo;
        return new RepresentanteLegalDto
        {
            TipoDocumento = rep.TipoDocumento ?? string.Empty,
            NumeroDocumento = rep.NumeroDocumento ?? string.Empty,
            Nombre = rep.Nombre ?? string.Empty,
            Cargo = rep.Cargo,
            FechaInicioCargo = rep.FechaInicioCargo,
            NivelRiesgoTexto = riesgoTexto,
            NivelRiesgo = NivelRiesgoMapper.ParseRiesgo(riesgoTexto)
        };
    }

    private void MapearEmpresasRelacionadas(ReporteCrediticioDto reporte, EquifaxModuloData data)
    {
        var empRel = data.EmpresasRelacionadas?.EmpresaRelacionada;
        if (empRel == null) return;

        reporte.EmpresasRelacionadas = empRel
            .Select(e =>
            {
                var riesgoTexto = e.ScoreHistoricos?.ScoreActual?.Riesgo;
                return new EmpresaRelacionadaDto
                {
                    TipoDocumento = e.TipoDocumento ?? string.Empty,
                    NumeroDocumento = e.NumeroDocumento ?? string.Empty,
                    Nombre = e.Nombre ?? string.Empty,
                    Relacion = e.Relacion,
                    NivelRiesgoTexto = riesgoTexto,
                    NivelRiesgo = NivelRiesgoMapper.ParseRiesgo(riesgoTexto)
                };
            })
            .ToList();
    }

    private void MapearNivelRiesgoPrincipal(ReporteCrediticioDto reporte, EquifaxModuloData data)
    {
        var riesgoTexto = data.ResumenFlags?.ResumenComportamiento?.ResumenScoreHistorico?.ScoreActual?.Riesgo;
        if (string.IsNullOrEmpty(riesgoTexto))
            return;

        reporte.NivelRiesgoTexto = riesgoTexto;
        reporte.NivelRiesgo = NivelRiesgoMapper.ParseRiesgo(riesgoTexto);
    }
}
