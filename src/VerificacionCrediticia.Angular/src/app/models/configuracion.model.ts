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
