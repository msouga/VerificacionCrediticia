using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class ResultadoMotorReglas
{
    public Recomendacion Recomendacion { get; set; }
    public decimal PuntajeFinal { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }
    public List<ResultadoReglaAplicada> ReglasAplicadas { get; set; } = new();
    public List<ResultadoValidacionCruzada> ValidacionesCruzadas { get; set; } = new();
    public RecomendacionLineaCredito? LineaCredito { get; set; }
    public ResultadoExploracionDto? ExploracionRed { get; set; }
    public decimal PenalidadRed { get; set; }
    public string Resumen { get; set; } = string.Empty;
    public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;
}

public class ResultadoReglaAplicada
{
    public int ReglaId { get; set; }
    public string NombreRegla { get; set; } = string.Empty;
    public string CampoEvaluado { get; set; } = string.Empty;
    public string OperadorUtilizado { get; set; } = string.Empty;
    public decimal ValorEsperado { get; set; }
    public decimal? ValorReal { get; set; }
    public bool Cumplida { get; set; }
    public decimal Peso { get; set; }
    public ResultadoRegla ResultadoRegla { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}