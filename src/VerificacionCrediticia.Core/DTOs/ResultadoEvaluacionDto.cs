using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class ResultadoEvaluacionDto
{
    public string DniSolicitante { get; set; } = string.Empty;
    public string NombreSolicitante { get; set; } = string.Empty;
    public string RucEmpresa { get; set; } = string.Empty;
    public string RazonSocialEmpresa { get; set; } = string.Empty;

    public decimal ScoreFinal { get; set; }
    public DesgloseScoreDto DesgloseScore { get; set; } = new();
    public Recomendacion Recomendacion { get; set; }
    public string RecomendacionTexto => Recomendacion switch
    {
        Recomendacion.Aprobar => "APROBAR",
        Recomendacion.RevisarManualmente => "REVISAR MANUALMENTE",
        Recomendacion.Rechazar => "RECHAZAR",
        _ => "DESCONOCIDO"
    };

    public ResumenRedDto Resumen { get; set; } = new();
    public List<Alerta> Alertas { get; set; } = new();
    public Dictionary<string, NodoRed>? Grafo { get; set; }

    public DateTime FechaEvaluacion { get; set; }
}

public class DesgloseScoreDto
{
    public decimal ScoreBase { get; set; } = 100m;

    // Score del solicitante (persona principal)
    public decimal ScoreSolicitante { get; set; } = 100m;
    public decimal PenalizacionSolicitante { get; set; }
    public List<string> MotivosSolicitante { get; set; } = new();

    // Score de la empresa
    public decimal ScoreEmpresa { get; set; } = 100m;
    public decimal PenalizacionEmpresa { get; set; }
    public List<string> MotivosEmpresa { get; set; } = new();

    // Score de las relaciones (red)
    public decimal ScoreRelaciones { get; set; } = 100m;
    public decimal PenalizacionRelaciones { get; set; }
    public List<string> MotivosRelaciones { get; set; } = new();
    public int TotalRelacionesAnalizadas { get; set; }
    public int RelacionesConProblemas { get; set; }
}

public class ResumenRedDto
{
    public int TotalPersonasAnalizadas { get; set; }
    public int TotalEmpresasAnalizadas { get; set; }
    public int PersonasConProblemas { get; set; }
    public int EmpresasConProblemas { get; set; }
    public decimal? ScorePromedioRed { get; set; }
    public decimal MontoTotalDeudas { get; set; }
    public decimal MontoTotalDeudasVencidas { get; set; }
}
