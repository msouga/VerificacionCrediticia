using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class DocumentoProcesadoResumenDto
{
    public int Id { get; set; }
    public int? TipoDocumentoId { get; set; }
    public string CodigoTipoDocumento { get; set; } = string.Empty;
    public string NombreTipoDocumento { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime FechaProcesado { get; set; }
    public EstadoDocumento Estado { get; set; }
    public decimal? ConfianzaPromedio { get; set; }
    public string? ErrorMensaje { get; set; }
}