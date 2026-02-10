export interface DocumentoIdentidad {
  nombres: string | null;
  apellidos: string | null;
  numeroDocumento: string | null;
  fechaNacimiento: string | null;
  fechaExpiracion: string | null;
  sexo: string | null;
  estadoCivil: string | null;
  direccion: string | null;
  nacionalidad: string | null;
  tipoDocumento: string | null;
  confianza: Record<string, number>;
  confianzaPromedio: number;
  archivoOrigen: string | null;
  dniValidado: boolean | null;
  mensajeValidacion: string | null;
}
