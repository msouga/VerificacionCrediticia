namespace VerificacionCrediticia.Core.DTOs;

public class TipoDocumentoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? AnalyzerId { get; set; }
    public bool EsObligatorio { get; set; }
    public bool Activo { get; set; }
    public int Orden { get; set; }
    public string? Descripcion { get; set; }
}

public class ActualizarTipoDocumentoRequest
{
    public bool EsObligatorio { get; set; }
    public bool Activo { get; set; }
    public int Orden { get; set; }
    public string? Descripcion { get; set; }
    public string? AnalyzerId { get; set; }
}