using System.Text.Json.Serialization;

namespace VerificacionCrediticia.Infrastructure.Equifax.Models;

public class EquifaxCreditReportResponse
{
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("applicants")]
    public EquifaxApplicants? Applicants { get; set; }
}

public class EquifaxApplicants
{
    [JsonPropertyName("primaryConsumer")]
    public EquifaxPrimaryConsumer? PrimaryConsumer { get; set; }
}

public class EquifaxPrimaryConsumer
{
    [JsonPropertyName("personalInformation")]
    public EquifaxPersonalInformation? PersonalInformation { get; set; }

    [JsonPropertyName("interconnectResponse")]
    public List<EquifaxModulo>? InterconnectResponse { get; set; }
}

public class EquifaxPersonalInformation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("tipoPersona")]
    public string? TipoPersona { get; set; }

    [JsonPropertyName("tipoDocumento")]
    public string? TipoDocumento { get; set; }
}

public class EquifaxModulo
{
    [JsonPropertyName("Codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("Data")]
    public EquifaxModuloData? Data { get; set; }
}

public class EquifaxModuloData
{
    [JsonPropertyName("flag")]
    public bool Flag { get; set; }

    [JsonPropertyName("RepresentantesLegales")]
    public EquifaxRepresentantesLegales? RepresentantesLegales { get; set; }

    [JsonPropertyName("EmpresasRelacionadas")]
    public EquifaxEmpresasRelacionadas? EmpresasRelacionadas { get; set; }

    [JsonPropertyName("DirectorioSUNAT")]
    public EquifaxDirectorioSunat? DirectorioSUNAT { get; set; }

    [JsonPropertyName("DirectorioPersona")]
    public EquifaxDirectorioPersona? DirectorioPersona { get; set; }

    [JsonPropertyName("ResumenConsulta")]
    public EquifaxResumenConsulta? ResumenConsulta { get; set; }
}

public class EquifaxRepresentantesLegales
{
    [JsonPropertyName("RepresentadoPor")]
    public EquifaxRepresentadoPorContainer? RepresentadoPor { get; set; }

    [JsonPropertyName("RepresentantesDe")]
    public EquifaxRepresentantesDeContainer? RepresentantesDe { get; set; }
}

public class EquifaxRepresentadoPorContainer
{
    [JsonPropertyName("RepresentadoPor")]
    public List<EquifaxRepresentanteLegal>? RepresentadoPor { get; set; }
}

public class EquifaxRepresentantesDeContainer
{
    [JsonPropertyName("RepresentantesDe")]
    public List<EquifaxRepresentanteLegal>? RepresentantesDe { get; set; }
}

public class EquifaxRepresentanteLegal
{
    [JsonPropertyName("TipoDocumento")]
    public string? TipoDocumento { get; set; }

    [JsonPropertyName("NumeroDocumento")]
    public string? NumeroDocumento { get; set; }

    [JsonPropertyName("FechaInicioCargo")]
    public string? FechaInicioCargo { get; set; }

    [JsonPropertyName("Cargo")]
    public string? Cargo { get; set; }

    [JsonPropertyName("Nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("ScoreHistoricos")]
    public EquifaxScoreHistoricos? ScoreHistoricos { get; set; }
}

public class EquifaxScoreHistoricos
{
    [JsonPropertyName("ScoreActual")]
    public EquifaxScorePeriodo? ScoreActual { get; set; }

    [JsonPropertyName("ScoreAnterior")]
    public EquifaxScorePeriodo? ScoreAnterior { get; set; }

    [JsonPropertyName("ScoreHace12Meses")]
    public EquifaxScorePeriodo? ScoreHace12Meses { get; set; }
}

public class EquifaxScorePeriodo
{
    [JsonPropertyName("Periodo")]
    public string? Periodo { get; set; }

    [JsonPropertyName("Riesgo")]
    public string? Riesgo { get; set; }
}

public class EquifaxEmpresasRelacionadas
{
    [JsonPropertyName("EmpresaRelacionada")]
    public List<EquifaxEmpresaRelacionada>? EmpresaRelacionada { get; set; }
}

public class EquifaxEmpresaRelacionada
{
    [JsonPropertyName("TipoDocumento")]
    public string? TipoDocumento { get; set; }

    [JsonPropertyName("NumeroDocumento")]
    public string? NumeroDocumento { get; set; }

    [JsonPropertyName("Nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("Relacion")]
    public string? Relacion { get; set; }

    [JsonPropertyName("ScoreHistoricos")]
    public EquifaxScoreHistoricos? ScoreHistoricos { get; set; }
}

public class EquifaxDirectorioSunat
{
    [JsonPropertyName("Directorio")]
    public List<EquifaxDirectorioSunatEntry>? Directorio { get; set; }
}

public class EquifaxDirectorioSunatEntry
{
    [JsonPropertyName("RUC")]
    public string? RUC { get; set; }

    [JsonPropertyName("RazonSocial")]
    public string? RazonSocial { get; set; }

    [JsonPropertyName("NombreComercial")]
    public string? NombreComercial { get; set; }

    [JsonPropertyName("TipoContribuyente")]
    public string? TipoContribuyente { get; set; }

    [JsonPropertyName("EstadoContribuyente")]
    public string? EstadoContribuyente { get; set; }

    [JsonPropertyName("CondicionContribuyente")]
    public string? CondicionContribuyente { get; set; }

    [JsonPropertyName("InicioActividades")]
    public string? InicioActividades { get; set; }

    [JsonPropertyName("NumeroTrabajadores")]
    public int? NumeroTrabajadores { get; set; }
}

public class EquifaxDirectorioPersona
{
    [JsonPropertyName("Nombres")]
    public string? Nombres { get; set; }

    [JsonPropertyName("TipoDocumento")]
    public string? TipoDocumento { get; set; }

    [JsonPropertyName("NumeroDocumento")]
    public string? NumeroDocumento { get; set; }

    [JsonPropertyName("FechaNacimiento")]
    public string? FechaNacimiento { get; set; }

    [JsonPropertyName("EstadoCivil")]
    public string? EstadoCivil { get; set; }

    [JsonPropertyName("Nacionalidad")]
    public string? Nacionalidad { get; set; }

    [JsonPropertyName("ApellidoPaterno")]
    public string? ApellidoPaterno { get; set; }

    [JsonPropertyName("ApellidoMaterno")]
    public string? ApellidoMaterno { get; set; }

    [JsonPropertyName("PrimerNombre")]
    public string? PrimerNombre { get; set; }

    [JsonPropertyName("SegundoNombre")]
    public string? SegundoNombre { get; set; }
}

public class EquifaxResumenConsulta
{
    [JsonPropertyName("TieneError")]
    public bool TieneError { get; set; }

    [JsonPropertyName("DetallesError")]
    public EquifaxDetallesError? DetallesError { get; set; }
}

public class EquifaxDetallesError
{
    [JsonPropertyName("CodigoError")]
    public int? CodigoError { get; set; }

    [JsonPropertyName("MensajeError")]
    public string? MensajeError { get; set; }
}
