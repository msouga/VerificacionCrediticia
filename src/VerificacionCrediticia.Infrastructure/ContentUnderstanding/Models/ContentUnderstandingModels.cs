using System.Text.Json.Serialization;

namespace VerificacionCrediticia.Infrastructure.ContentUnderstanding.Models;

public class AnalyzeResultResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public AnalyzeResult? Result { get; set; }
}

public class AnalyzeResult
{
    [JsonPropertyName("analyzerId")]
    public string AnalyzerId { get; set; } = string.Empty;

    [JsonPropertyName("contents")]
    public List<AnalyzeContent> Contents { get; set; } = new();
}

public class AnalyzeContent
{
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, AnalyzeField>? Fields { get; set; }

    [JsonPropertyName("markdown")]
    public string? Markdown { get; set; }
}

public class AnalyzeField
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("valueString")]
    public string? ValueString { get; set; }

    [JsonPropertyName("valueDate")]
    public string? ValueDate { get; set; }

    [JsonPropertyName("confidence")]
    public float? Confidence { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("valueArray")]
    public List<AnalyzeArrayItem>? ValueArray { get; set; }

    [JsonPropertyName("valueObject")]
    public Dictionary<string, AnalyzeField>? ValueObject { get; set; }
}

public class AnalyzeArrayItem
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("valueObject")]
    public Dictionary<string, AnalyzeField>? ValueObject { get; set; }
}
