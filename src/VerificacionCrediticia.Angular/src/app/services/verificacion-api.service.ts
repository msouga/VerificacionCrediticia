import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { SolicitudVerificacion } from '../models/solicitud-verificacion.model';
import { ResultadoEvaluacion } from '../models/resultado-evaluacion.model';
import { DocumentoIdentidad } from '../models/documento-identidad.model';
import { VigenciaPoder } from '../models/vigencia-poder.model';
import { BalanceGeneral } from '../models/balance-general.model';
import { EstadoResultados } from '../models/estado-resultados.model';
import {
  Expediente, CrearExpedienteRequest, TipoDocumento
} from '../models/expediente.model';
import {
  TipoDocumentoConfig, ActualizarTipoDocumentoRequest
} from '../models/configuracion.model';

@Injectable({ providedIn: 'root' })
export class VerificacionApiService {
  private baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient, private zone: NgZone) {}

  evaluar(solicitud: SolicitudVerificacion): Observable<ResultadoEvaluacion> {
    return this.http.post<ResultadoEvaluacion>(
      `${this.baseUrl}/api/verificacion/evaluar`,
      solicitud
    );
  }

  consultarPersona(dni: string): Observable<any> {
    return this.http.get(
      `${this.baseUrl}/api/verificacion/persona/${dni}`
    );
  }

  consultarEmpresa(ruc: string): Observable<any> {
    return this.http.get(
      `${this.baseUrl}/api/verificacion/empresa/${ruc}`
    );
  }

  procesarDni(archivo: File): Observable<DocumentoIdentidad> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);
    return this.http.post<DocumentoIdentidad>(
      `${this.baseUrl}/api/documentos/dni`,
      formData
    );
  }

  procesarVigenciaPoder(
    archivo: File,
    onProgress: (mensaje: string) => void
  ): Promise<VigenciaPoder> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);

    return new Promise<VigenciaPoder>(async (resolve, reject) => {
      try {
        const response = await fetch(
          `${this.baseUrl}/api/documentos/vigencia-poder`,
          { method: 'POST', body: formData }
        );

        if (!response.ok) {
          const error = await response.json();
          reject(new Error(error.detail || error.title || 'Error al procesar'));
          return;
        }

        const reader = response.body?.getReader();
        if (!reader) {
          reject(new Error('No se pudo leer la respuesta'));
          return;
        }

        const decoder = new TextDecoder();
        let buffer = '';
        let resultado: VigenciaPoder | null = null;

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() || '';

          for (const line of lines) {
            if (line.startsWith('event: ')) {
              const eventType = line.substring(7).trim();
              // El siguiente "data:" viene en la siguiente linea procesada
              // Lo manejamos con un flag temporal
              (this as any)._currentEvent = eventType;
            } else if (line.startsWith('data: ')) {
              const data = line.substring(6);
              const eventType = (this as any)._currentEvent || 'progress';
              (this as any)._currentEvent = null;

              if (eventType === 'progress') {
                this.zone.run(() => onProgress(data));
              } else if (eventType === 'result') {
                resultado = JSON.parse(data) as VigenciaPoder;
              } else if (eventType === 'error') {
                reject(new Error(data));
                return;
              }
            }
          }
        }

        if (resultado) {
          resolve(resultado);
        } else {
          reject(new Error('No se recibio resultado del servidor'));
        }
      } catch (err: any) {
        reject(new Error(err.message || 'Error de conexion'));
      }
    });
  }

  procesarBalanceGeneral(
    archivo: File,
    onProgress: (mensaje: string) => void
  ): Promise<BalanceGeneral> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);

    return new Promise<BalanceGeneral>(async (resolve, reject) => {
      try {
        const response = await fetch(
          `${this.baseUrl}/api/documentos/balance-general`,
          { method: 'POST', body: formData }
        );

        if (!response.ok) {
          const error = await response.json();
          reject(new Error(error.detail || error.title || 'Error al procesar'));
          return;
        }

        const reader = response.body?.getReader();
        if (!reader) {
          reject(new Error('No se pudo leer la respuesta'));
          return;
        }

        const decoder = new TextDecoder();
        let buffer = '';
        let resultado: BalanceGeneral | null = null;

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() || '';

          for (const line of lines) {
            if (line.startsWith('event: ')) {
              const eventType = line.substring(7).trim();
              (this as any)._currentEvent = eventType;
            } else if (line.startsWith('data: ')) {
              const data = line.substring(6);
              const eventType = (this as any)._currentEvent || 'progress';
              (this as any)._currentEvent = null;

              if (eventType === 'progress') {
                this.zone.run(() => onProgress(data));
              } else if (eventType === 'result') {
                resultado = JSON.parse(data) as BalanceGeneral;
              } else if (eventType === 'error') {
                reject(new Error(data));
                return;
              }
            }
          }
        }

        if (resultado) {
          resolve(resultado);
        } else {
          reject(new Error('No se recibio resultado del servidor'));
        }
      } catch (err: any) {
        reject(new Error(err.message || 'Error de conexion'));
      }
    });
  }

  // Expedientes

  crearExpediente(request: CrearExpedienteRequest): Observable<Expediente> {
    return this.http.post<Expediente>(
      `${this.baseUrl}/api/expedientes`,
      request
    );
  }

  getExpediente(id: number): Observable<Expediente> {
    return this.http.get<Expediente>(
      `${this.baseUrl}/api/expedientes/${id}`
    );
  }

  getTiposDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<TipoDocumento[]>(
      `${this.baseUrl}/api/expedientes/tipos-documento`
    );
  }

  // Configuracion

  getTiposDocumentoConfig(): Observable<TipoDocumentoConfig[]> {
    return this.http.get<TipoDocumentoConfig[]>(
      `${this.baseUrl}/api/configuracion/tipos-documento`
    );
  }

  actualizarTipoDocumento(id: number, data: ActualizarTipoDocumentoRequest): Observable<TipoDocumentoConfig> {
    return this.http.put<TipoDocumentoConfig>(
      `${this.baseUrl}/api/configuracion/tipos-documento/${id}`,
      data
    );
  }

  evaluarExpediente(id: number): Observable<Expediente> {
    return this.http.post<Expediente>(
      `${this.baseUrl}/api/expedientes/${id}/evaluar`,
      {}
    );
  }

  procesarDocumentoExpediente(
    expedienteId: number,
    codigoTipo: string,
    archivo: File,
    onProgress: (mensaje: string) => void
  ): Promise<void> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);

    return this.procesarSSE<void>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/${codigoTipo}`,
      formData,
      onProgress,
      () => { /* result se ignora, recargamos el expediente completo */ }
    );
  }

  reemplazarDocumentoExpediente(
    expedienteId: number,
    documentoId: number,
    archivo: File,
    onProgress: (mensaje: string) => void
  ): Promise<void> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);

    return this.procesarSSE<void>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/${documentoId}`,
      formData,
      onProgress,
      () => { /* result se ignora */ },
      'PUT'
    );
  }

  private procesarSSE<T>(
    url: string,
    formData: FormData,
    onProgress: (mensaje: string) => void,
    onResult: (data: T) => void,
    method: string = 'POST'
  ): Promise<T> {
    return new Promise<T>(async (resolve, reject) => {
      try {
        const response = await fetch(url, { method, body: formData });

        if (!response.ok) {
          const error = await response.json();
          reject(new Error(error.detail || error.title || 'Error al procesar'));
          return;
        }

        const reader = response.body?.getReader();
        if (!reader) {
          reject(new Error('No se pudo leer la respuesta'));
          return;
        }

        const decoder = new TextDecoder();
        let buffer = '';
        let currentEvent = '';
        let resultado: T | null = null;

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() || '';

          for (const line of lines) {
            if (line.startsWith('event: ')) {
              currentEvent = line.substring(7).trim();
            } else if (line.startsWith('data: ')) {
              const data = line.substring(6);
              const eventType = currentEvent || 'progress';
              currentEvent = '';

              if (eventType === 'progress') {
                this.zone.run(() => onProgress(data));
              } else if (eventType === 'result') {
                try {
                  resultado = JSON.parse(data) as T;
                  onResult(resultado);
                } catch {
                  // El resultado podría no ser JSON si T es void
                }
              } else if (eventType === 'error') {
                reject(new Error(data));
                return;
              }
            }
          }
        }

        resolve(resultado as T);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Error de conexion';
        reject(new Error(message));
      }
    });
  }

  procesarEstadoResultados(
    archivo: File,
    onProgress: (mensaje: string) => void
  ): Promise<EstadoResultados> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);

    return new Promise<EstadoResultados>(async (resolve, reject) => {
      try {
        const response = await fetch(
          `${this.baseUrl}/api/documentos/estado-resultados`,
          { method: 'POST', body: formData }
        );

        if (!response.ok) {
          const error = await response.json();
          reject(new Error(error.detail || error.title || 'Error al procesar'));
          return;
        }

        const reader = response.body?.getReader();
        if (!reader) {
          reject(new Error('No se pudo leer la respuesta'));
          return;
        }

        const decoder = new TextDecoder();
        let buffer = '';
        let resultado: EstadoResultados | null = null;

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() || '';

          for (const line of lines) {
            if (line.startsWith('event: ')) {
              const eventType = line.substring(7).trim();
              (this as any)._currentEvent = eventType;
            } else if (line.startsWith('data: ')) {
              const data = line.substring(6);
              const eventType = (this as any)._currentEvent || 'progress';
              (this as any)._currentEvent = null;

              if (eventType === 'progress') {
                this.zone.run(() => onProgress(data));
              } else if (eventType === 'result') {
                resultado = JSON.parse(data) as EstadoResultados;
              } else if (eventType === 'error') {
                reject(new Error(data));
                return;
              }
            }
          }
        }

        if (resultado) {
          resolve(resultado);
        } else {
          reject(new Error('No se recibio resultado del servidor'));
        }
      } catch (err: any) {
        reject(new Error(err.message || 'Error de conexion'));
      }
    });
  }
}
