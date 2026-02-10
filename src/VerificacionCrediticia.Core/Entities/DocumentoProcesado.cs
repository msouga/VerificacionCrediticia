using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class DocumentoProcesado
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    public int TipoDocumentoId { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public DateTime FechaProcesado { get; set; } = DateTime.UtcNow;

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Pendiente;

    public string? DatosExtraidosJson { get; set; }

    public decimal? ConfianzaPromedio { get; set; }

    public string? ErrorMensaje { get; set; }

    public string? BlobUri { get; set; }

    // Navegacion
    public virtual Expediente Expediente { get; set; } = null!;
    public virtual TipoDocumento TipoDocumento { get; set; } = null!;
}
