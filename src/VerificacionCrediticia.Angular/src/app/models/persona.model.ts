import { EstadoCrediticio, NivelRiesgo } from './enums';

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

export interface Persona {
  dni: string;
  nombres: string;
  nombreCompleto: string;
  nivelRiesgo: NivelRiesgo;
  nivelRiesgoTexto?: string;
  estado: EstadoCrediticio;
  deudas: DeudaRegistrada[];
  fechaConsulta?: string;
}
