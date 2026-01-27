using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Empresa
{
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Estado { get; set; }
    public string? Direccion { get; set; }
    public decimal? ScoreCrediticio { get; set; }
    public EstadoCrediticio EstadoCredito { get; set; } = EstadoCrediticio.SinInformacion;
    public List<DeudaRegistrada> Deudas { get; set; } = new();
    public List<RelacionSocietaria> Socios { get; set; } = new();
    public DateTime? FechaConsulta { get; set; }
}
