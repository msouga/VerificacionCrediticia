import { EstadoCrediticio, NivelRiesgo } from './enums';
import { DeudaRegistrada } from './persona.model';

export interface Empresa {
  ruc: string;
  razonSocial: string;
  nombreComercial?: string;
  tipoContribuyente?: string;
  estadoContribuyente?: string;
  condicionContribuyente?: string;
  nivelRiesgo: NivelRiesgo;
  nivelRiesgoTexto?: string;
  estadoCredito: EstadoCrediticio;
  deudas: DeudaRegistrada[];
  fechaConsulta?: string;
}
