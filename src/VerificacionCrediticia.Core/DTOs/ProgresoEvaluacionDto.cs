namespace VerificacionCrediticia.Core.DTOs;

public class ProgresoEvaluacionDto
{
    public string Archivo { get; set; } = string.Empty;
    public string Paso { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public int DocumentoActual { get; set; }
    public int TotalDocumentos { get; set; }
}
