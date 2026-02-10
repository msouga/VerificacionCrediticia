namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Datos extraidos de un Balance General procesado por Content Understanding
/// </summary>
public class BalanceGeneralDto
{
    #region Encabezado
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? Domicilio { get; set; }
    public string? FechaBalance { get; set; }
    public string? Moneda { get; set; }
    #endregion

    #region Activo Corriente
    public decimal? EfectivoEquivalentes { get; set; }
    public decimal? CuentasCobrarComerciales { get; set; }
    public decimal? CuentasCobrarDiversas { get; set; }
    public decimal? Existencias { get; set; }
    public decimal? GastosPagadosAnticipado { get; set; }
    public decimal? TotalActivoCorriente { get; set; }
    #endregion

    #region Activo No Corriente
    public decimal? InmueblesMaquinariaEquipo { get; set; }
    public decimal? DepreciacionAcumulada { get; set; }
    public decimal? Intangibles { get; set; }
    public decimal? AmortizacionAcumulada { get; set; }
    public decimal? ActivoDiferido { get; set; }
    public decimal? TotalActivoNoCorriente { get; set; }
    #endregion

    #region Total Activo
    public decimal? TotalActivo { get; set; }
    #endregion

    #region Pasivo Corriente
    public decimal? TributosPorPagar { get; set; }
    public decimal? RemuneracionesPorPagar { get; set; }
    public decimal? CuentasPagarComerciales { get; set; }
    public decimal? ObligacionesFinancierasCorto { get; set; }
    public decimal? OtrasCuentasPorPagar { get; set; }
    public decimal? TotalPasivoCorriente { get; set; }
    #endregion

    #region Pasivo No Corriente
    public decimal? ObligacionesFinancierasLargo { get; set; }
    public decimal? Provisiones { get; set; }
    public decimal? TotalPasivoNoCorriente { get; set; }
    #endregion

    #region Total Pasivo
    public decimal? TotalPasivo { get; set; }
    #endregion

    #region Patrimonio
    public decimal? CapitalSocial { get; set; }
    public decimal? ReservaLegal { get; set; }
    public decimal? ResultadosAcumulados { get; set; }
    public decimal? ResultadoEjercicio { get; set; }
    public decimal? TotalPatrimonio { get; set; }
    #endregion

    #region Total Pasivo + Patrimonio
    public decimal? TotalPasivoPatrimonio { get; set; }
    #endregion

    #region Firmantes
    /// <summary>
    /// Lista de firmantes del balance (contador, gerente, etc.)
    /// </summary>
    public List<FirmanteDto> Firmantes { get; set; } = new();
    #endregion

    #region Metadata
    /// <summary>
    /// Confidence scores por campo (0.0 a 1.0)
    /// </summary>
    public Dictionary<string, float> Confianza { get; set; } = new();

    /// <summary>
    /// Confianza promedio general del documento
    /// </summary>
    public float ConfianzaPromedio { get; set; }

    /// <summary>
    /// Nombre del archivo original procesado
    /// </summary>
    public string? ArchivoOrigen { get; set; }

    /// <summary>
    /// Resultado de la validacion del RUC contra Equifax (null si no se ha validado)
    /// </summary>
    public bool? RucValidado { get; set; }

    /// <summary>
    /// Mensaje de la validacion del RUC
    /// </summary>
    public string? MensajeValidacionRuc { get; set; }
    #endregion

    #region Ratios Calculados
    /// <summary>
    /// Ratio de Liquidez = Activo Corriente / Pasivo Corriente
    /// </summary>
    public decimal? RatioLiquidez { get; set; }

    /// <summary>
    /// Ratio de Endeudamiento = Total Pasivo / Total Activo
    /// </summary>
    public decimal? RatioEndeudamiento { get; set; }

    /// <summary>
    /// Ratio de Solvencia = Total Patrimonio / Total Activo
    /// </summary>
    public decimal? RatioSolvencia { get; set; }

    /// <summary>
    /// Capital de Trabajo = Activo Corriente - Pasivo Corriente
    /// </summary>
    public decimal? CapitalTrabajo { get; set; }
    #endregion
}