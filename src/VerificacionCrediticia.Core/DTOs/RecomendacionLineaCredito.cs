namespace VerificacionCrediticia.Core.DTOs;

public class RecomendacionLineaCredito
{
    public decimal MontoMaximoSugerido { get; set; }
    public string Moneda { get; set; } = "PEN";
    public string Justificacion { get; set; } = string.Empty;
    public List<DetalleCalculoLinea> Detalles { get; set; } = new();
}

public class DetalleCalculoLinea
{
    public string Concepto { get; set; } = string.Empty;
    public decimal? ValorBase { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal? MontoCalculado { get; set; }
}
