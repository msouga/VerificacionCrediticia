using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class ExpedienteDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? DniSolicitante { get; set; }
    public string? NombresSolicitante { get; set; }
    public string? ApellidosSolicitante { get; set; }
    public string? RucEmpresa { get; set; }
    public string? RazonSocialEmpresa { get; set; }
    public EstadoExpediente Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEvaluacion { get; set; }

    // Documentos procesados
    public List<DocumentoProcesadoResumenDto> Documentos { get; set; } = new();

    // Tipos de documento requeridos (para saber cuales faltan)
    public List<TipoDocumentoDto> TiposDocumentoRequeridos { get; set; } = new();

    // Contadores de progreso
    public int DocumentosObligatoriosCompletos { get; set; }
    public int TotalDocumentosObligatorios { get; set; }
    public bool PuedeEvaluar { get; set; }

    // Resultado de evaluacion (si existe)
    public ResultadoEvaluacionExpedienteDto? ResultadoEvaluacion { get; set; }
}

public class ResultadoEvaluacionExpedienteDto
{
    public decimal ScoreFinal { get; set; }
    public Recomendacion Recomendacion { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }
    public string Resumen { get; set; } = string.Empty;
    public List<ReglaAplicadaExpedienteDto> ReglasAplicadas { get; set; } = new();
    public DateTime FechaEvaluacion { get; set; }
}

public class ReglaAplicadaExpedienteDto
{
    public string NombreRegla { get; set; } = string.Empty;
    public string CampoEvaluado { get; set; } = string.Empty;
    public string Operador { get; set; } = string.Empty;
    public decimal ValorEsperado { get; set; }
    public decimal? ValorReal { get; set; }
    public bool Cumplida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public ResultadoRegla ResultadoRegla { get; set; }
}
