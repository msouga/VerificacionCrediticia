using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Empresa
{
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? TipoContribuyente { get; set; }
    public string? EstadoContribuyente { get; set; }
    public string? CondicionContribuyente { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }
    public string? NivelRiesgoTexto { get; set; }
    public EstadoCrediticio EstadoCredito { get; set; } = EstadoCrediticio.SinInformacion;
    public List<DeudaRegistrada> Deudas { get; set; } = new();
    public DateTime? FechaConsulta { get; set; }
}
