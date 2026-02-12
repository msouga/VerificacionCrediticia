namespace VerificacionCrediticia.Core.DTOs;

public class EstadisticasExpedientesDto
{
    public int TotalExpedientes { get; set; }
    public int Evaluados { get; set; }
    public int EnProceso { get; set; }
    public int Aprobados { get; set; }
    public int EnRevision { get; set; }
    public int Rechazados { get; set; }
    public decimal ScorePromedio { get; set; }
    public List<ExpedienteEvaluadoResumenDto> Recientes { get; set; } = new();
}

public class ExpedienteEvaluadoResumenDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? DniSolicitante { get; set; }
    public string? NombresSolicitante { get; set; }
    public string? ApellidosSolicitante { get; set; }
    public string? RucEmpresa { get; set; }
    public string? RazonSocialEmpresa { get; set; }
    public decimal ScoreFinal { get; set; }
    public int Recomendacion { get; set; }
    public DateTime FechaEvaluacion { get; set; }
}
