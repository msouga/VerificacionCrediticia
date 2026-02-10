namespace VerificacionCrediticia.Infrastructure.ContentUnderstanding;

public class ContentUnderstandingSettings
{
    public const string SectionName = "ContentUnderstanding";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AnalyzerId { get; set; } = "dniperuano";
    public string VigenciaPoderesAnalyzerId { get; set; } = "vigenciaPoderes";
    public string BalanceGeneralAnalyzerId { get; set; } = "balanceGeneral";
    public string EstadoResultadosAnalyzerId { get; set; } = "estadoResultados";
    public string ApiVersion { get; set; } = "2025-11-01";
    public bool UseMock { get; set; } = true;
    public int PollingIntervalMs { get; set; } = 1000;
    public int PollingTimeoutMs { get; set; } = 60000;
}
