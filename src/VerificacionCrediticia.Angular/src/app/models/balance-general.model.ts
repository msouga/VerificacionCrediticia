export interface Firmante {
  nombre?: string;
  dni?: string;
  cargo?: string;
  matricula?: string;
  dniValidado?: boolean;
  mensajeValidacion?: string;
}

export interface BalanceGeneral {
  // Encabezado
  ruc?: string;
  razonSocial?: string;
  domicilio?: string;
  fechaBalance?: string;
  moneda?: string;

  // Activo Corriente
  efectivoEquivalentes?: number;
  cuentasCobrarComerciales?: number;
  cuentasCobrarDiversas?: number;
  existencias?: number;
  gastosPagadosAnticipado?: number;
  totalActivoCorriente?: number;

  // Activo No Corriente
  inmueblesMaquinariaEquipo?: number;
  depreciacionAcumulada?: number;
  intangibles?: number;
  amortizacionAcumulada?: number;
  activoDiferido?: number;
  totalActivoNoCorriente?: number;

  // Total Activo
  totalActivo?: number;

  // Pasivo Corriente
  tributosPorPagar?: number;
  remuneracionesPorPagar?: number;
  cuentasPagarComerciales?: number;
  obligacionesFinancierasCorto?: number;
  otrasCuentasPorPagar?: number;
  totalPasivoCorriente?: number;

  // Pasivo No Corriente
  obligacionesFinancierasLargo?: number;
  provisiones?: number;
  totalPasivoNoCorriente?: number;

  // Total Pasivo
  totalPasivo?: number;

  // Patrimonio
  capitalSocial?: number;
  reservaLegal?: number;
  resultadosAcumulados?: number;
  resultadoEjercicio?: number;
  totalPatrimonio?: number;

  // Total Pasivo + Patrimonio
  totalPasivoPatrimonio?: number;

  // Firmantes
  firmantes: Firmante[];

  // Metadata
  confianza: { [key: string]: number };
  confianzaPromedio: number;
  archivoOrigen?: string;
  rucValidado?: boolean;
  mensajeValidacionRuc?: string;

  // Ratios Calculados
  ratioLiquidez?: number;
  ratioEndeudamiento?: number;
  ratioSolvencia?: number;
  capitalTrabajo?: number;
}