namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Datos de un firmante extraido de un Balance General
/// </summary>
public class FirmanteDto
{
    public string? Nombre { get; set; }
    public string? Dni { get; set; }
    public string? Cargo { get; set; }
    public string? Matricula { get; set; }

    /// <summary>
    /// Resultado de la validacion RENIEC (null si no se ha validado)
    /// </summary>
    public bool? DniValidado { get; set; }

    /// <summary>
    /// Mensaje de la validacion RENIEC
    /// </summary>
    public string? MensajeValidacion { get; set; }
}