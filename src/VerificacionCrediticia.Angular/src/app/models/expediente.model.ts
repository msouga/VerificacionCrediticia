export enum EstadoExpediente {
  Iniciado = 0,
  EnProceso = 1,
  DocumentosCompletos = 2,
  Evaluado = 3
}

export enum EstadoDocumento {
  Pendiente = 0,
  Procesando = 1,
  Procesado = 2,
  Error = 3,
  Subido = 4
}

export interface ProgresoEvaluacion {
  archivo: string;
  paso: string;
  detalle?: string;
  documentoActual: number;
  totalDocumentos: number;
}

export enum ResultadoRegla {
  Aprobar = 0,
  Rechazar = 1,
  Revisar = 2
}

export interface CrearExpedienteRequest {
  descripcion: string;
}

export interface ActualizarExpedienteRequest {
  descripcion: string;
}

export interface TipoDocumento {
  id: number;
  nombre: string;
  codigo: string;
  esObligatorio: boolean;
  orden: number;
  descripcion: string;
}

export interface DocumentoProcesadoResumen {
  id: number;
  tipoDocumentoId: number | null;
  codigoTipoDocumento: string;
  nombreTipoDocumento: string;
  nombreArchivo: string;
  fechaProcesado: string;
  estado: EstadoDocumento;
  confianzaPromedio: number | null;
  errorMensaje: string | null;
}

export interface ResultadoValidacionCruzada {
  nombre: string;
  aprobada: boolean;
  severidad: ResultadoRegla;
  mensaje: string;
  documentosInvolucrados: string[];
}

export interface ReglaAplicadaExpediente {
  nombreRegla: string;
  campoEvaluado: string;
  operador: string;
  valorEsperado: number;
  valorReal: number | null;
  cumplida: boolean;
  mensaje: string;
  resultadoRegla: ResultadoRegla;
}

export interface DetalleCalculoLinea {
  concepto: string;
  valorBase: number | null;
  porcentaje: number;
  montoCalculado: number | null;
}

export interface RecomendacionLineaCredito {
  montoMaximoSugerido: number;
  moneda: string;
  justificacion: string;
  detalles: DetalleCalculoLinea[];
}

export enum TipoNodo {
  Persona = 0,
  Empresa = 1
}

export interface DeudaRegistrada {
  entidad: string;
  tipoDeuda: string;
  montoOriginal: number;
  saldoActual: number;
  diasVencidos: number;
  calificacion: string;
  fechaVencimiento: string | null;
  estaVencida: boolean;
}

export interface ConexionNodo {
  identificador: string;
  tipo: TipoNodo;
  nombre: string;
  tipoRelacion: string;
}

export interface NodoRed {
  identificador: string;
  tipo: TipoNodo;
  nombre: string;
  nivelProfundidad: number;
  score: number | null;
  nivelRiesgoTexto: string | null;
  estadoCredito: number;
  alertas: string[];
  deudas: DeudaRegistrada[];
  conexiones: ConexionNodo[];
}

export interface ResultadoExploracionRed {
  dniSolicitante: string;
  rucEmpresa: string;
  grafo: { [key: string]: NodoRed };
  totalNodos: number;
  totalPersonas: number;
  totalEmpresas: number;
  fechaConsulta: string;
}

export interface ResultadoEvaluacionExpediente {
  scoreFinal: number;
  recomendacion: number;
  nivelRiesgo: number;
  resumen: string;
  penalidadRed: number;
  reglasAplicadas: ReglaAplicadaExpediente[];
  validacionesCruzadas: ResultadoValidacionCruzada[];
  lineaCredito: RecomendacionLineaCredito | null;
  exploracionRed: ResultadoExploracionRed | null;
  fechaEvaluacion: string;
}

export interface Expediente {
  id: number;
  descripcion: string;
  dniSolicitante: string | null;
  nombresSolicitante: string | null;
  apellidosSolicitante: string | null;
  rucEmpresa: string | null;
  razonSocialEmpresa: string | null;
  estado: EstadoExpediente;
  fechaCreacion: string;
  fechaEvaluacion: string | null;
  documentos: DocumentoProcesadoResumen[];
  tiposDocumentoRequeridos: TipoDocumento[];
  documentosObligatoriosCompletos: number;
  totalDocumentosObligatorios: number;
  puedeEvaluar: boolean;
  resultadoEvaluacion: ResultadoEvaluacionExpediente | null;
}

export interface ExpedienteResumen {
  id: number;
  descripcion: string;
  dniSolicitante: string | null;
  nombresSolicitante: string | null;
  rucEmpresa: string | null;
  razonSocialEmpresa: string | null;
  estado: EstadoExpediente;
  fechaCreacion: string;
  documentosObligatoriosCompletos: number;
  totalDocumentosObligatorios: number;
}

export interface ListaExpedientesResponse {
  items: ExpedienteResumen[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
