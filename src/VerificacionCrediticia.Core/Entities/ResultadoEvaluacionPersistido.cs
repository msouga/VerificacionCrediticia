using System.ComponentModel.DataAnnotations;
using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class ResultadoEvaluacionPersistido
{
    public int Id { get; set; }

    public int ExpedienteId { get; set; }

    public decimal ScoreFinal { get; set; }

    public Recomendacion Recomendacion { get; set; }

    public NivelRiesgo NivelRiesgo { get; set; }

    public string ResultadoCompletoJson { get; set; } = string.Empty;

    public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public virtual Expediente Expediente { get; set; } = null!;
}