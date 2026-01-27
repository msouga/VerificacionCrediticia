using System.Text.Json.Serialization;

namespace VerificacionCrediticia.Infrastructure.Equifax.Models;

public class EquifaxPersonaResponse
{
    [JsonPropertyName("dni")]
    public string? Dni { get; set; }

    [JsonPropertyName("nombres")]
    public string? Nombres { get; set; }

    [JsonPropertyName("apellidos")]
    public string? Apellidos { get; set; }

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonPropertyName("calificacion")]
    public string? Calificacion { get; set; }

    [JsonPropertyName("deudas")]
    public List<EquifaxDeudaResponse>? Deudas { get; set; }

    [JsonPropertyName("empresasRelacionadas")]
    public List<EquifaxRelacionResponse>? EmpresasRelacionadas { get; set; }
}

public class EquifaxEmpresaResponse
{
    [JsonPropertyName("ruc")]
    public string? Ruc { get; set; }

    [JsonPropertyName("razonSocial")]
    public string? RazonSocial { get; set; }

    [JsonPropertyName("nombreComercial")]
    public string? NombreComercial { get; set; }

    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonPropertyName("calificacion")]
    public string? Calificacion { get; set; }

    [JsonPropertyName("deudas")]
    public List<EquifaxDeudaResponse>? Deudas { get; set; }

    [JsonPropertyName("socios")]
    public List<EquifaxRelacionResponse>? Socios { get; set; }
}

public class EquifaxDeudaResponse
{
    [JsonPropertyName("entidad")]
    public string? Entidad { get; set; }

    [JsonPropertyName("tipoDeuda")]
    public string? TipoDeuda { get; set; }

    [JsonPropertyName("montoOriginal")]
    public decimal MontoOriginal { get; set; }

    [JsonPropertyName("saldoActual")]
    public decimal SaldoActual { get; set; }

    [JsonPropertyName("diasVencidos")]
    public int DiasVencidos { get; set; }

    [JsonPropertyName("calificacion")]
    public string? Calificacion { get; set; }

    [JsonPropertyName("fechaVencimiento")]
    public DateTime? FechaVencimiento { get; set; }
}

public class EquifaxRelacionResponse
{
    [JsonPropertyName("dni")]
    public string? Dni { get; set; }

    [JsonPropertyName("nombrePersona")]
    public string? NombrePersona { get; set; }

    [JsonPropertyName("ruc")]
    public string? Ruc { get; set; }

    [JsonPropertyName("razonSocial")]
    public string? RazonSocial { get; set; }

    [JsonPropertyName("tipoRelacion")]
    public string? TipoRelacion { get; set; }

    [JsonPropertyName("porcentajeParticipacion")]
    public decimal? PorcentajeParticipacion { get; set; }

    [JsonPropertyName("fechaInicio")]
    public DateTime? FechaInicio { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; } = true;
}
