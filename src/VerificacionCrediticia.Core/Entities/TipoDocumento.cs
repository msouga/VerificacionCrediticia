namespace VerificacionCrediticia.Core.Entities;

public class TipoDocumento
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public string? AnalyzerId { get; set; }

    public bool EsObligatorio { get; set; }

    public int Orden { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegacion
    public virtual ICollection<DocumentoProcesado> DocumentosProcesados { get; set; } = new List<DocumentoProcesado>();
}
