namespace VerificacionCrediticia.Core.DTOs;

public class TipoDocumentoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsObligatorio { get; set; }
    public int Orden { get; set; }
    public string? Descripcion { get; set; }
}