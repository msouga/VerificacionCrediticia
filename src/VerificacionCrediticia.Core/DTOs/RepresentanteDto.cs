namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Datos de un representante legal extraido de una Vigencia de Poder
/// </summary>
public class RepresentanteDto
{
    public string? Nombre { get; set; }
    public string? DocumentoIdentidad { get; set; }
    public string? Cargo { get; set; }
    public string? FechaNombramiento { get; set; }
    public string? Facultades { get; set; }

    /// <summary>
    /// Resultado de la validacion RENIEC (null si no se ha validado)
    /// </summary>
    public bool? DniValidado { get; set; }

    /// <summary>
    /// Mensaje de la validacion RENIEC
    /// </summary>
    public string? MensajeValidacion { get; set; }
}
