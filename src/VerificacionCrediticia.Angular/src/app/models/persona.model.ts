import { EstadoCrediticio } from './enums';

export interface DeudaRegistrada {
  entidad: string;
  tipoDeuda: string;
  montoOriginal: number;
  saldoActual: number;
  diasVencidos: number;
  calificacion: string;
  fechaVencimiento?: string;
  estaVencida: boolean;
}

export interface RelacionSocietaria {
  dni: string;
  nombrePersona: string;
  ruc: string;
  razonSocialEmpresa: string;
  tipoRelacion: string;
  porcentajeParticipacion?: number;
  fechaInicio?: string;
  esActiva: boolean;
}

export interface Persona {
  dni: string;
  nombres: string;
  apellidos: string;
  nombreCompleto: string;
  scoreCrediticio?: number;
  estado: EstadoCrediticio;
  deudas: DeudaRegistrada[];
  empresasDondeEsSocio: RelacionSocietaria[];
  fechaConsulta?: string;
}
