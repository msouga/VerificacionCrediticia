export interface Representante {
  nombre: string | null;
  documentoIdentidad: string | null;
  cargo: string | null;
  fechaNombramiento: string | null;
  facultades: string | null;
  dniValidado: boolean | null;
  mensajeValidacion: string | null;
}

export interface VigenciaPoder {
  ruc: string | null;
  razonSocial: string | null;
  tipoPersonaJuridica: string | null;
  domicilio: string | null;
  objetoSocial: string | null;
  capitalSocial: string | null;
  partidaRegistral: string | null;
  fechaConstitucion: string | null;
  representantes: Representante[];
  confianza: Record<string, number>;
  confianzaPromedio: number;
  archivoOrigen: string | null;
  rucValidado: boolean | null;
  mensajeValidacionRuc: string | null;
}
