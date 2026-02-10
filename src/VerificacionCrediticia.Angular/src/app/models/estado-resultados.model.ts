export interface EstadoResultados {
  // Encabezado
  ruc?: string;
  razonSocial?: string;
  periodo?: string;
  moneda?: string;

  // Partidas del Estado de Resultados
  ventasNetas?: number;
  costoVentas?: number;
  utilidadBruta?: number;
  gastosAdministrativos?: number;
  gastosVentas?: number;
  utilidadOperativa?: number;
  otrosIngresos?: number;
  otrosGastos?: number;
  utilidadAntesImpuestos?: number;
  impuestoRenta?: number;
  utilidadNeta?: number;

  // Ratios calculados
  margenBruto?: number;
  margenOperativo?: number;
  margenNeto?: number;

  // Metadata
  confianzaPromedio: number;
  datosValidosRuc: boolean;
  fechaProcesado: string;
}

export interface EstadoResultadosResponse {
  success: boolean;
  data?: EstadoResultados;
  error?: string;
  timestamp: string;
}