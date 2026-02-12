namespace VerificacionCrediticia.Core.Entities;

public class ParametroLineaCredito
{
    public int Id { get; set; }
    public decimal PorcentajeCapitalTrabajo { get; set; } = 20m;
    public decimal PorcentajePatrimonio { get; set; } = 30m;
    public decimal PorcentajeUtilidadNeta { get; set; } = 100m;

    // Pesos de penalidad por red de relaciones (%)
    public decimal PesoRedNivel0 { get; set; } = 100m;
    public decimal PesoRedNivel1 { get; set; } = 50m;
    public decimal PesoRedNivel2 { get; set; } = 25m;
}
