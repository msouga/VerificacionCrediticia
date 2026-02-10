using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Expediente
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public string? DniSolicitante { get; set; }

    public string? NombresSolicitante { get; set; }

    public string? ApellidosSolicitante { get; set; }

    public string? RucEmpresa { get; set; }

    public string? RazonSocialEmpresa { get; set; }

    public EstadoExpediente Estado { get; set; } = EstadoExpediente.Iniciado;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaEvaluacion { get; set; }

    // Navegacion
    public virtual ICollection<DocumentoProcesado> Documentos { get; set; } = new List<DocumentoProcesado>();
    public virtual ResultadoEvaluacionPersistido? ResultadoEvaluacion { get; set; }
}
