using System.ComponentModel.DataAnnotations;
using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Expediente
{
    public int Id { get; set; }

    [Required]
    [MaxLength(8)]
    public string DniSolicitante { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombresSolicitante { get; set; }

    [MaxLength(100)]
    public string? ApellidosSolicitante { get; set; }

    [MaxLength(11)]
    public string? RucEmpresa { get; set; }

    [MaxLength(200)]
    public string? RazonSocialEmpresa { get; set; }

    public EstadoExpediente Estado { get; set; } = EstadoExpediente.Iniciado;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaEvaluacion { get; set; }

    // Navegación
    public virtual ICollection<DocumentoProcesado> Documentos { get; set; } = new List<DocumentoProcesado>();
    public virtual ResultadoEvaluacionPersistido? ResultadoEvaluacion { get; set; }
}