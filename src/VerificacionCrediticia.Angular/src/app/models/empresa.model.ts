import { EstadoCrediticio } from './enums';
import { DeudaRegistrada, RelacionSocietaria } from './persona.model';

export interface Empresa {
  ruc: string;
  razonSocial: string;
  nombreComercial?: string;
  estado?: string;
  direccion?: string;
  scoreCrediticio?: number;
  estadoCredito: EstadoCrediticio;
  deudas: DeudaRegistrada[];
  socios: RelacionSocietaria[];
  fechaConsulta?: string;
}
