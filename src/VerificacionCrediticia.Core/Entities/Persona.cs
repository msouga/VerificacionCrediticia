using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Persona
{
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string NombreCompleto => Nombres;
    public NivelRiesgo NivelRiesgo { get; set; }
    public string? NivelRiesgoTexto { get; set; }
    public EstadoCrediticio Estado { get; set; } = EstadoCrediticio.SinInformacion;
    public List<DeudaRegistrada> Deudas { get; set; } = new();
    public DateTime? FechaConsulta { get; set; }
}
