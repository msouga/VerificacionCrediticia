using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class ExpedienteResumenDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? DniSolicitante { get; set; }
    public string? NombresSolicitante { get; set; }
    public string? RucEmpresa { get; set; }
    public string? RazonSocialEmpresa { get; set; }
    public EstadoExpediente Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int DocumentosObligatoriosCompletos { get; set; }
    public int TotalDocumentosObligatorios { get; set; }
}
