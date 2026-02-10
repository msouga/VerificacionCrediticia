namespace VerificacionCrediticia.Infrastructure.Reniec;

public class ReniecSettings
{
    public const string SectionName = "Reniec";

    public bool UseMock { get; set; } = true;
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
