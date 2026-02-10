import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { SolicitudVerificacion } from '../models/solicitud-verificacion.model';
import { ResultadoEvaluacion } from '../models/resultado-evaluacion.model';
import { DocumentoIdentidad } from '../models/documento-identidad.model';
import { VigenciaPoder } from '../models/vigencia-poder.model';

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
}
