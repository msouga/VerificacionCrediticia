using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Persona
{
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public decimal? ScoreCrediticio { get; set; }
    public EstadoCrediticio Estado { get; set; } = EstadoCrediticio.SinInformacion;
    public List<DeudaRegistrada> Deudas { get; set; } = new();
    public List<RelacionSocietaria> EmpresasDondeEsSocio { get; set; } = new();
    public DateTime? FechaConsulta { get; set; }
}
