using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class RelacionRepresentacion
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoRelacion { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? FechaInicioCargo { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }
    public string? NivelRiesgoTexto { get; set; }
}
