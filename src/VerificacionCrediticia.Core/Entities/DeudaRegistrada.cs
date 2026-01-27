namespace VerificacionCrediticia.Core.Entities;

public class DeudaRegistrada
{
    public string Entidad { get; set; } = string.Empty;
    public string TipoDeuda { get; set; } = string.Empty;
    public decimal MontoOriginal { get; set; }
    public decimal SaldoActual { get; set; }
    public int DiasVencidos { get; set; }
    public string Calificacion { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public bool EstaVencida => DiasVencidos > 0;
}
