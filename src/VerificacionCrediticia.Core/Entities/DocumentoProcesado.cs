using System.ComponentModel.DataAnnotations;
using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class DocumentoProcesado
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    public int TipoDocumentoId { get; set; }

    [Required]
    [MaxLength(255)]
    public string NombreArchivo { get; set; } = string.Empty;

    public DateTime FechaProcesado { get; set; } = DateTime.UtcNow;

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Pendiente;

    public string? DatosExtraidosJson { get; set; }

    public decimal? ConfianzaPromedio { get; set; }

    [MaxLength(1000)]
    public string? ErrorMensaje { get; set; }

    [MaxLength(500)]
    public string? BlobUri { get; set; }

    // Navegación
    public virtual Expediente Expediente { get; set; } = null!;
    public virtual TipoDocumento TipoDocumento { get; set; } = null!;
}