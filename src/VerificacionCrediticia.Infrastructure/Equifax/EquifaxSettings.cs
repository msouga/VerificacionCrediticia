namespace VerificacionCrediticia.Infrastructure.Equifax;

public class EquifaxSettings
{
    public const string SectionName = "Equifax";

    public string BaseUrl { get; set; } = "https://api.equifax.com";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int CacheMinutes { get; set; } = 60;
    public bool UseSandbox { get; set; } = true;
    public bool UseMock { get; set; } = false;
    public string? BillTo { get; set; }
    public string? ShipTo { get; set; }

    public string EffectiveBaseUrl => UseSandbox
        ? "https://api.sandbox.equifax.com"
        : BaseUrl;
}
