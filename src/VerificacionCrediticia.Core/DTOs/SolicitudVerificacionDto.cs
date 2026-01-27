namespace VerificacionCrediticia.Core.DTOs;

public class SolicitudVerificacionDto
{
    public string DniSolicitante { get; set; } = string.Empty;
    public string RucEmpresa { get; set; } = string.Empty;
    public int ProfundidadMaxima { get; set; } = 2;
    public bool IncluirDetalleGrafo { get; set; } = true;
}
