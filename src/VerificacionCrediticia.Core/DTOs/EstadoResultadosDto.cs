using System.ComponentModel.DataAnnotations;

namespace VerificacionCrediticia.Core.DTOs;

public class EstadoResultadosDto
{
    // Encabezado
    [MaxLength(11)]
    public string? Ruc { get; set; }

    [MaxLength(200)]
    public string? RazonSocial { get; set; }

    [MaxLength(50)]
    public string? Periodo { get; set; }

    [MaxLength(20)]
    public string? Moneda { get; set; }

    // Partidas del Estado de Resultados
    public decimal? VentasNetas { get; set; }
    public decimal? CostoVentas { get; set; }
    public decimal? UtilidadBruta { get; set; }
    public decimal? GastosAdministrativos { get; set; }
    public decimal? GastosVentas { get; set; }
    public decimal? UtilidadOperativa { get; set; }
    public decimal? OtrosIngresos { get; set; }
    public decimal? OtrosGastos { get; set; }
    public decimal? UtilidadAntesImpuestos { get; set; }
    public decimal? ImpuestoRenta { get; set; }
    public decimal? UtilidadNeta { get; set; }

    // Ratios calculados
    public decimal? MargenBruto { get; set; }
    public decimal? MargenOperativo { get; set; }
    public decimal? MargenNeto { get; set; }

    // Metadata
    public decimal ConfianzaPromedio { get; set; }
    public bool DatosValidosRuc { get; set; }
    public DateTime FechaProcesado { get; set; }

    // Método para calcular ratios
    public void CalcularRatios()
    {
        if (VentasNetas.HasValue && VentasNetas > 0)
        {
            MargenBruto = UtilidadBruta.HasValue ? (UtilidadBruta / VentasNetas) * 100 : null;
            MargenOperativo = UtilidadOperativa.HasValue ? (UtilidadOperativa / VentasNetas) * 100 : null;
            MargenNeto = UtilidadNeta.HasValue ? (UtilidadNeta / VentasNetas) * 100 : null;
        }
        else
        {
            MargenBruto = null;
            MargenOperativo = null;
            MargenNeto = null;
        }
    }
}