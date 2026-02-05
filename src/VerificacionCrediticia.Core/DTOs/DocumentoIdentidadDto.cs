namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Datos extraidos de un documento de identidad (DNI) procesado por Document Intelligence
/// </summary>
public class DocumentoIdentidadDto
{
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? FechaNacimiento { get; set; }
    public string? FechaExpiracion { get; set; }
    public string? Sexo { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Direccion { get; set; }
    public string? Nacionalidad { get; set; }
    public string? TipoDocumento { get; set; }

    /// <summary>
    /// Confidence scores por campo (0.0 a 1.0)
    /// </summary>
    public Dictionary<string, float> Confianza { get; set; } = new();

    /// <summary>
    /// Confianza promedio general del documento
    /// </summary>
    public float ConfianzaPromedio { get; set; }

    /// <summary>
    /// Nombre del archivo original procesado
    /// </summary>
    public string? ArchivoOrigen { get; set; }
}
