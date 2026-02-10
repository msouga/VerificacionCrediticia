namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Datos extraidos de una Vigencia de Poder procesada por Content Understanding
/// </summary>
public class VigenciaPoderDto
{
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? TipoPersonaJuridica { get; set; }
    public string? Domicilio { get; set; }
    public string? ObjetoSocial { get; set; }
    public string? CapitalSocial { get; set; }
    public string? PartidaRegistral { get; set; }
    public string? FechaConstitucion { get; set; }

    /// <summary>
    /// Lista de representantes legales
    /// </summary>
    public List<RepresentanteDto> Representantes { get; set; } = new();

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

    /// <summary>
    /// Resultado de la validacion del RUC contra Equifax (null si no se ha validado)
    /// </summary>
    public bool? RucValidado { get; set; }

    /// <summary>
    /// Mensaje de la validacion del RUC
    /// </summary>
    public string? MensajeValidacionRuc { get; set; }
}
