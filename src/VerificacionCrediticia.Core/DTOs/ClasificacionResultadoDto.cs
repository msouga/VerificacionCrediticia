namespace VerificacionCrediticia.Core.DTOs;

public class ClasificacionResultadoDto
{
    public string CategoriaDetectada { get; set; } = string.Empty;
    public object? ResultadoExtraccion { get; set; }
    public decimal ConfianzaClasificacion { get; set; }
}
