import { Recomendacion, EstadoCrediticio, TipoNodo } from './enums';
import { Alerta } from './alerta.model';
import { DeudaRegistrada } from './persona.model';

export interface DesgloseScore {
  scoreBase: number;
  scoreSolicitante: number;
  penalizacionSolicitante: number;
  motivosSolicitante: string[];
  scoreEmpresa: number;
  penalizacionEmpresa: number;
  motivosEmpresa: string[];
  scoreRelaciones: number;
  penalizacionRelaciones: number;
  motivosRelaciones: string[];
  totalRelacionesAnalizadas: number;
  relacionesConProblemas: number;
}

export interface ResumenRed {
  totalPersonasAnalizadas: number;
  totalEmpresasAnalizadas: number;
  personasConProblemas: number;
  empresasConProblemas: number;
  scorePromedioRed?: number;
  montoTotalDeudas: number;
  montoTotalDeudasVencidas: number;
}

export interface ConexionNodo {
  identificador: string;
  tipo: TipoNodo;
  nombre: string;
  tipoRelacion: string;
}

export interface NodoRed {
  identificador: string;
  tipo: TipoNodo;
  nombre: string;
  nivelProfundidad: number;
  score?: number;
  nivelRiesgoTexto?: string;
  estadoCredito: EstadoCrediticio;
  alertas: string[];
  deudas: DeudaRegistrada[];
  conexiones: ConexionNodo[];
}

export interface ResultadoEvaluacion {
  dniSolicitante: string;
  nombreSolicitante: string;
  rucEmpresa: string;
  razonSocialEmpresa: string;
  scoreFinal: number;
  desgloseScore: DesgloseScore;
  recomendacion: Recomendacion;
  resumen: ResumenRed;
  alertas: Alerta[];
  grafo?: Record<string, NodoRed>;
  fechaEvaluacion: string;
}
