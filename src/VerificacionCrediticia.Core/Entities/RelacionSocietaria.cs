namespace VerificacionCrediticia.Core.Entities;

public class RelacionSocietaria
{
    public string Dni { get; set; } = string.Empty;
    public string NombrePersona { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocialEmpresa { get; set; } = string.Empty;
    public string TipoRelacion { get; set; } = string.Empty;
    public decimal? PorcentajeParticipacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public bool EsActiva { get; set; } = true;
}
