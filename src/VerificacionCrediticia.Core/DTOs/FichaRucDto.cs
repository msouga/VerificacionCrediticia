namespace VerificacionCrediticia.Core.DTOs;

public class FichaRucDto
{
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? TipoContribuyente { get; set; }
    public string? FechaInscripcion { get; set; }
    public string? FechaInicioActividades { get; set; }
    public string? EstadoContribuyente { get; set; }
    public string? CondicionDomicilio { get; set; }
    public string? DomicilioFiscal { get; set; }
    public string? ActividadEconomica { get; set; }
    public string? SistemaContabilidad { get; set; }
    public string? ComprobantesAutorizados { get; set; }

    public Dictionary<string, float> Confianza { get; set; } = new();
    public float ConfianzaPromedio { get; set; }
    public string? ArchivoOrigen { get; set; }
}
