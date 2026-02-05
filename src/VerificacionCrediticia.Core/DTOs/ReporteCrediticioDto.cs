using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class ReporteCrediticioDto
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;

    public DatosPersonaDto? DatosPersona { get; set; }
    public DatosEmpresaDto? DatosEmpresa { get; set; }

    public string? NivelRiesgoTexto { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }

    public List<RepresentanteLegalDto> RepresentadoPor { get; set; } = new();
    public List<RepresentanteLegalDto> RepresentantesDe { get; set; } = new();
    public List<EmpresaRelacionadaDto> EmpresasRelacionadas { get; set; } = new();
    public List<DeudaRegistrada> Deudas { get; set; } = new();
}

public class DatosPersonaDto
{
    public string Nombres { get; set; } = string.Empty;
    public string? FechaNacimiento { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Nacionalidad { get; set; }
}

public class DatosEmpresaDto
{
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? TipoContribuyente { get; set; }
    public string? EstadoContribuyente { get; set; }
    public string? CondicionContribuyente { get; set; }
    public string? InicioActividades { get; set; }
}

public class RepresentanteLegalDto
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? FechaInicioCargo { get; set; }
    public string? NivelRiesgoTexto { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }
}

public class EmpresaRelacionadaDto
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Relacion { get; set; }
    public string? NivelRiesgoTexto { get; set; }
    public NivelRiesgo NivelRiesgo { get; set; }
}
