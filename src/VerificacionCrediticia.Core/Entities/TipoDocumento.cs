using System.ComponentModel.DataAnnotations;

namespace VerificacionCrediticia.Core.Entities;

public class TipoDocumento
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? AnalyzerId { get; set; }

    public bool EsObligatorio { get; set; }

    public int Orden { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public virtual ICollection<DocumentoProcesado> DocumentosProcesados { get; set; } = new List<DocumentoProcesado>();
}