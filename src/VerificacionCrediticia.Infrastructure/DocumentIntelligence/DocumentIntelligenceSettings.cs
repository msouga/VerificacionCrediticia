namespace VerificacionCrediticia.Infrastructure.DocumentIntelligence;

public class DocumentIntelligenceSettings
{
    public const string SectionName = "DocumentIntelligence";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
