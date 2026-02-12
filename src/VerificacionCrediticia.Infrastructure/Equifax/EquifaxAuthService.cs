using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VerificacionCrediticia.Infrastructure.Equifax;

public interface IEquifaxAuthService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public class EquifaxAuthService : IEquifaxAuthService
{
    private readonly HttpClient _httpClient;
    private readonly EquifaxSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EquifaxAuthService> _logger;
    private const string TokenCacheKey = "equifax_access_token";

    public EquifaxAuthService(
        HttpClient httpClient,
        IOptions<EquifaxSettings> settings,
        IMemoryCache cache,
        ILogger<EquifaxAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        var token = await RequestNewTokenAsync(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(55)); // Token típicamente dura 1 hora

        _cache.Set(TokenCacheKey, token, cacheOptions);

        return token;
    }

    private async Task<string> RequestNewTokenAsync(CancellationToken cancellationToken)
    {
        var tokenUrl = $"{_settings.EffectiveBaseUrl}/v2/oauth/token";

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", _settings.Scope)
        });
        request.Content = content;

        // === DEBUG: Capturar auth request ===
        _logger.LogDebug("=== EQUIFAX AUTH REQUEST ===");
        _logger.LogDebug("URL: POST {Url}", tokenUrl);
        _logger.LogDebug("Scope: {Scope}", _settings.Scope);
        _logger.LogDebug("Auth Headers:");
        foreach (var h in request.Headers)
            _logger.LogDebug("  {Key}: {Value}", h.Key,
                h.Key == "Authorization" ? h.Value.First()[..20] + "..." : string.Join(", ", h.Value));

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // === DEBUG: Capturar auth response ===
        _logger.LogDebug("=== EQUIFAX AUTH RESPONSE ===");
        _logger.LogDebug("Status: {StatusCode} ({StatusInt})", response.StatusCode, (int)response.StatusCode);
        _logger.LogDebug("Response Headers:");
        foreach (var h in response.Headers)
            _logger.LogDebug("  {Key}: {Value}", h.Key, string.Join(", ", h.Value));
        // No logear el token completo por seguridad, solo primeros chars
        var safeResponse = responseContent.Length > 200 ? responseContent[..200] + "..." : responseContent;
        _logger.LogDebug("Response Body (truncado): {Body}", safeResponse);

        response.EnsureSuccessStatusCode();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("No se pudo obtener el token de acceso de Equifax");
        }

        return tokenResponse.AccessToken;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
