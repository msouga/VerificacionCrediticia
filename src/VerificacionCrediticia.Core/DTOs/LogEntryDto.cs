namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Entrada de log enviada desde el frontend
/// </summary>
public class LogEntryDto
{
    public string? Nivel { get; set; }
    public string? Mensaje { get; set; }
    public string? Origen { get; set; }
    public Dictionary<string, object>? Datos { get; set; }
    public string? StackTrace { get; set; }
}
