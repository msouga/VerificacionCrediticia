import { Severidad, TipoAlerta, TipoNodo } from './enums';

export interface Alerta {
  tipo: TipoAlerta;
  mensaje: string;
  severidad: Severidad;
  nivelProfundidad: number;
  identificadorEntidad?: string;
  tipoEntidad?: TipoNodo;
}
