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
  Error = 3
}

export enum ResultadoRegla {
  Aprobar = 0,
  Rechazar = 1,
  Revisar = 2
}

export interface CrearExpedienteRequest {
  dniSolicitante: string;
  rucEmpresa?: string;
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
  tipoDocumentoId: number;
  codigoTipoDocumento: string;
  nombreTipoDocumento: string;
  nombreArchivo: string;
  fechaProcesado: string;
  estado: EstadoDocumento;
  confianzaPromedio: number | null;
  errorMensaje: string | null;
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

export interface ResultadoEvaluacionExpediente {
  scoreFinal: number;
  recomendacion: number;
  nivelRiesgo: number;
  resumen: string;
  reglasAplicadas: ReglaAplicadaExpediente[];
  fechaEvaluacion: string;
}

export interface Expediente {
  id: number;
  dniSolicitante: string;
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
