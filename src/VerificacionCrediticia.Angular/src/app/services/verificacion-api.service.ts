import { Injectable, NgZone, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  Expediente, CrearExpedienteRequest, ActualizarExpedienteRequest,
  TipoDocumento, ListaExpedientesResponse,
  DocumentoProcesadoResumen, ProgresoEvaluacion
} from '../models/expediente.model';
import {
  TipoDocumentoConfig, ActualizarTipoDocumentoRequest,
  ReglaEvaluacionConfig, CrearReglaRequest, ActualizarReglaRequest,
  ParametrosLineaCredito
} from '../models/configuracion.model';

@Injectable({ providedIn: 'root' })
export class VerificacionApiService {
  private http = inject(HttpClient);
  private zone = inject(NgZone);

  private baseUrl = environment.apiBaseUrl;

  // Expedientes

  crearExpediente(request: CrearExpedienteRequest): Observable<Expediente> {
    return this.http.post<Expediente>(
      `${this.baseUrl}/api/expedientes`,
      request
    );
  }

  listarExpedientes(pagina: number, tamanoPagina: number): Observable<ListaExpedientesResponse> {
    return this.http.get<ListaExpedientesResponse>(
      `${this.baseUrl}/api/expedientes`,
      { params: { pagina: pagina.toString(), tamanoPagina: tamanoPagina.toString() } }
    );
  }

  actualizarExpediente(id: number, request: ActualizarExpedienteRequest): Observable<Expediente> {
    return this.http.put<Expediente>(
      `${this.baseUrl}/api/expedientes/${id}`,
      request
    );
  }

  eliminarExpedientes(ids: number[]): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/api/expedientes`,
      { body: { ids } }
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

  // Reglas de evaluacion

  getReglas(): Observable<ReglaEvaluacionConfig[]> {
    return this.http.get<ReglaEvaluacionConfig[]>(
      `${this.baseUrl}/api/configuracion/reglas`
    );
  }

  crearRegla(request: CrearReglaRequest): Observable<ReglaEvaluacionConfig> {
    return this.http.post<ReglaEvaluacionConfig>(
      `${this.baseUrl}/api/configuracion/reglas`,
      request
    );
  }

  actualizarRegla(id: number, request: ActualizarReglaRequest): Observable<ReglaEvaluacionConfig> {
    return this.http.put<ReglaEvaluacionConfig>(
      `${this.baseUrl}/api/configuracion/reglas/${id}`,
      request
    );
  }

  eliminarRegla(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/api/configuracion/reglas/${id}`
    );
  }

  // Parametros de linea de credito
  getParametrosLineaCredito(): Observable<ParametrosLineaCredito> {
    return this.http.get<ParametrosLineaCredito>(
      `${this.baseUrl}/api/configuracion/linea-credito`
    );
  }

  actualizarParametrosLineaCredito(params: ParametrosLineaCredito): Observable<ParametrosLineaCredito> {
    return this.http.put<ParametrosLineaCredito>(
      `${this.baseUrl}/api/configuracion/linea-credito`,
      params
    );
  }

  // Upload simple (sin SSE) - solo sube archivo al blob
  subirDocumento(
    expedienteId: number,
    codigoTipo: string,
    archivo: File
  ): Observable<DocumentoProcesadoResumen> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);
    return this.http.post<DocumentoProcesadoResumen>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/${codigoTipo}`,
      formData
    );
  }

  // Upload masivo (sin SSE) - sube multiples archivos para clasificacion automatica
  subirDocumentosBulk(
    expedienteId: number,
    archivos: File[]
  ): Observable<DocumentoProcesadoResumen[]> {
    const formData = new FormData();
    for (const archivo of archivos) {
      formData.append('archivos', archivo, archivo.name);
    }
    return this.http.post<DocumentoProcesadoResumen[]>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/bulk`,
      formData
    );
  }

  // Descartar documento en error
  descartarDocumento(expedienteId: number, documentoId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/${documentoId}`
    );
  }

  // Aceptar documento en error como correcto y re-encolar para procesamiento
  aceptarDocumento(expedienteId: number, documentoId: number): Observable<DocumentoProcesadoResumen> {
    return this.http.post<DocumentoProcesadoResumen>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/${documentoId}/aceptar`,
      {}
    );
  }

  // Reemplazar documento (sin SSE) - solo reemplaza archivo en blob
  reemplazarDocumento(
    expedienteId: number,
    documentoId: number,
    archivo: File
  ): Observable<DocumentoProcesadoResumen> {
    const formData = new FormData();
    formData.append('archivo', archivo, archivo.name);
    return this.http.put<DocumentoProcesadoResumen>(
      `${this.baseUrl}/api/expedientes/${expedienteId}/documentos/${documentoId}`,
      formData
    );
  }

  // Evaluar expediente con SSE (procesa documentos + reglas)
  async evaluarExpedienteSSE(
    expedienteId: number,
    callbacks: {
      onProgress: (progreso: ProgresoEvaluacion) => void;
      onResult: (expediente: Expediente) => void;
      onError: (error: string) => void;
    },
    abortSignal?: AbortSignal
  ): Promise<void> {
    try {
      const response = await fetch(
        `${this.baseUrl}/api/expedientes/${expedienteId}/evaluar`,
        {
          method: 'POST',
          signal: abortSignal
        }
      );

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.detail || error.title || 'Error al evaluar');
      }

      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error('No se pudo leer la respuesta');
      }

      const decoder = new TextDecoder();
      let buffer = '';
      let currentEvent = '';

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
              try {
                const progreso = JSON.parse(data) as ProgresoEvaluacion;
                this.zone.run(() => callbacks.onProgress(progreso));
              } catch {
                // Ignorar si no es JSON valido
              }
            } else if (eventType === 'result') {
              const expediente = JSON.parse(data) as Expediente;
              this.zone.run(() => callbacks.onResult(expediente));
            } else if (eventType === 'error') {
              this.zone.run(() => callbacks.onError(data));
              throw new Error(data);
            }
          }
        }
      }
    } catch (err: unknown) {
      if (err instanceof DOMException && err.name === 'AbortError') {
        return; // Cancelacion no es error
      }
      const message = err instanceof Error ? err.message : 'Error de conexion';
      throw new Error(message);
    }
  }

}
