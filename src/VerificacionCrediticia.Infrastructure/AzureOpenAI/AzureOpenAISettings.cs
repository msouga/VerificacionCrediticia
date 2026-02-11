namespace VerificacionCrediticia.Infrastructure.AzureOpenAI;

public class AzureOpenAISettings
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-41";
    public string ApiVersion { get; set; } = "2024-12-01-preview";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0;
    public int ImageDpi { get; set; } = 200;
    public int MaxPagesPerDocument { get; set; } = 10;
    public bool UseMock { get; set; } = true;
}
