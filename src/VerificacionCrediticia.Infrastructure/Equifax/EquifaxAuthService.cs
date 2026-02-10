using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
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
    private const string TokenCacheKey = "equifax_access_token";

    public EquifaxAuthService(
        HttpClient httpClient,
        IOptions<EquifaxSettings> settings,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
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

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
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
