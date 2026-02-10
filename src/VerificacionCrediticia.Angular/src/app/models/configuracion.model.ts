export interface TipoDocumentoConfig {
  id: number;
  nombre: string;
  codigo: string;
  analyzerId: string | null;
  esObligatorio: boolean;
  activo: boolean;
  orden: number;
  descripcion: string | null;
}

export interface ActualizarTipoDocumentoRequest {
  esObligatorio: boolean;
  activo: boolean;
  orden: number;
  descripcion: string | null;
  analyzerId: string | null;
}

export interface ReglaEvaluacionConfig {
  id: number;
  nombre: string;
  descripcion: string | null;
  campo: string;
  operador: number;
  valor: number;
  peso: number;
  resultado: number;
  activa: boolean;
  orden: number;
}

export interface CrearReglaRequest {
  nombre: string;
  descripcion: string | null;
  campo: string;
  operador: number;
  valor: number;
  peso: number;
  resultado: number;
  orden: number;
}

export interface ActualizarReglaRequest {
  descripcion: string | null;
  operador: number;
  valor: number;
  peso: number;
  resultado: number;
  activa: boolean;
  orden: number;
}
