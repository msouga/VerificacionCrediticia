using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;
using VerificacionCrediticia.Infrastructure.Equifax.Models;

namespace VerificacionCrediticia.Infrastructure.Equifax;

public class EquifaxApiClient : IEquifaxApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IEquifaxAuthService _authService;
    private readonly EquifaxSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public EquifaxApiClient(
        HttpClient httpClient,
        IEquifaxAuthService authService,
        IOptions<EquifaxSettings> settings,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _authService = authService;
        _settings = settings.Value;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Persona?> ConsultarPersonaAsync(string dni, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"persona_{dni}";
        if (_cache.TryGetValue(cacheKey, out Persona? cachedPersona))
        {
            return cachedPersona;
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        var url = $"{_settings.EffectiveBaseUrl}/v1/credit/person/{dni}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var equifaxResponse = JsonSerializer.Deserialize<EquifaxPersonaResponse>(content, _jsonOptions);

        if (equifaxResponse == null) return null;

        var persona = MapearPersona(equifaxResponse);

        _cache.Set(cacheKey, persona, TimeSpan.FromMinutes(_settings.CacheMinutes));

        return persona;
    }

    public async Task<Empresa?> ConsultarEmpresaAsync(string ruc, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"empresa_{ruc}";
        if (_cache.TryGetValue(cacheKey, out Empresa? cachedEmpresa))
        {
            return cachedEmpresa;
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        var url = $"{_settings.EffectiveBaseUrl}/v1/credit/business/{ruc}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var equifaxResponse = JsonSerializer.Deserialize<EquifaxEmpresaResponse>(content, _jsonOptions);

        if (equifaxResponse == null) return null;

        var empresa = MapearEmpresa(equifaxResponse);

        _cache.Set(cacheKey, empresa, TimeSpan.FromMinutes(_settings.CacheMinutes));

        return empresa;
    }

    public async Task<List<RelacionSocietaria>> ObtenerEmpresasDondeEsSocioAsync(
        string dni,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"empresas_socio_{dni}";
        if (_cache.TryGetValue(cacheKey, out List<RelacionSocietaria>? cachedRelaciones))
        {
            return cachedRelaciones ?? new List<RelacionSocietaria>();
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        var url = $"{_settings.EffectiveBaseUrl}/v1/relations/person/{dni}/companies";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new List<RelacionSocietaria>();
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var equifaxResponse = JsonSerializer.Deserialize<List<EquifaxRelacionResponse>>(content, _jsonOptions);

        var relaciones = equifaxResponse?.Select(MapearRelacion).ToList() ?? new List<RelacionSocietaria>();

        _cache.Set(cacheKey, relaciones, TimeSpan.FromMinutes(_settings.CacheMinutes));

        return relaciones;
    }

    public async Task<List<RelacionSocietaria>> ObtenerSociosDeEmpresaAsync(
        string ruc,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"socios_empresa_{ruc}";
        if (_cache.TryGetValue(cacheKey, out List<RelacionSocietaria>? cachedRelaciones))
        {
            return cachedRelaciones ?? new List<RelacionSocietaria>();
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        var url = $"{_settings.EffectiveBaseUrl}/v1/relations/company/{ruc}/partners";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new List<RelacionSocietaria>();
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var equifaxResponse = JsonSerializer.Deserialize<List<EquifaxRelacionResponse>>(content, _jsonOptions);

        var relaciones = equifaxResponse?.Select(MapearRelacion).ToList() ?? new List<RelacionSocietaria>();

        _cache.Set(cacheKey, relaciones, TimeSpan.FromMinutes(_settings.CacheMinutes));

        return relaciones;
    }

    private Persona MapearPersona(EquifaxPersonaResponse response)
    {
        return new Persona
        {
            Dni = response.Dni ?? string.Empty,
            Nombres = response.Nombres ?? string.Empty,
            Apellidos = response.Apellidos ?? string.Empty,
            ScoreCrediticio = response.Score,
            Estado = MapearEstadoCrediticio(response.Calificacion),
            Deudas = response.Deudas?.Select(MapearDeuda).ToList() ?? new List<DeudaRegistrada>(),
            EmpresasDondeEsSocio = response.EmpresasRelacionadas?.Select(MapearRelacion).ToList()
                ?? new List<RelacionSocietaria>(),
            FechaConsulta = DateTime.UtcNow
        };
    }

    private Empresa MapearEmpresa(EquifaxEmpresaResponse response)
    {
        return new Empresa
        {
            Ruc = response.Ruc ?? string.Empty,
            RazonSocial = response.RazonSocial ?? string.Empty,
            NombreComercial = response.NombreComercial,
            Estado = response.Estado,
            Direccion = response.Direccion,
            ScoreCrediticio = response.Score,
            EstadoCredito = MapearEstadoCrediticio(response.Calificacion),
            Deudas = response.Deudas?.Select(MapearDeuda).ToList() ?? new List<DeudaRegistrada>(),
            Socios = response.Socios?.Select(MapearRelacion).ToList() ?? new List<RelacionSocietaria>(),
            FechaConsulta = DateTime.UtcNow
        };
    }

    private DeudaRegistrada MapearDeuda(EquifaxDeudaResponse response)
    {
        return new DeudaRegistrada
        {
            Entidad = response.Entidad ?? string.Empty,
            TipoDeuda = response.TipoDeuda ?? string.Empty,
            MontoOriginal = response.MontoOriginal,
            SaldoActual = response.SaldoActual,
            DiasVencidos = response.DiasVencidos,
            Calificacion = response.Calificacion ?? string.Empty,
            FechaVencimiento = response.FechaVencimiento
        };
    }

    private RelacionSocietaria MapearRelacion(EquifaxRelacionResponse response)
    {
        return new RelacionSocietaria
        {
            Dni = response.Dni ?? string.Empty,
            NombrePersona = response.NombrePersona ?? string.Empty,
            Ruc = response.Ruc ?? string.Empty,
            RazonSocialEmpresa = response.RazonSocial ?? string.Empty,
            TipoRelacion = response.TipoRelacion ?? string.Empty,
            PorcentajeParticipacion = response.PorcentajeParticipacion,
            FechaInicio = response.FechaInicio,
            EsActiva = response.Activo
        };
    }

    private EstadoCrediticio MapearEstadoCrediticio(string? calificacion)
    {
        return calificacion?.ToUpper() switch
        {
            "NORMAL" or "0" or "A" => EstadoCrediticio.Normal,
            "CPP" or "1" or "B" => EstadoCrediticio.ConProblemasPotenciales,
            "DEFICIENTE" or "2" or "C" => EstadoCrediticio.Moroso,
            "DUDOSO" or "3" or "D" => EstadoCrediticio.EnCobranza,
            "PERDIDA" or "4" or "E" => EstadoCrediticio.Castigado,
            _ => EstadoCrediticio.SinInformacion
        };
    }
}
