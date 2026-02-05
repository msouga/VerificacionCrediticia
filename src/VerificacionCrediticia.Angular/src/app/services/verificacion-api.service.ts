import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { SolicitudVerificacion } from '../models/solicitud-verificacion.model';
import { ResultadoEvaluacion } from '../models/resultado-evaluacion.model';

@Injectable({ providedIn: 'root' })
export class VerificacionApiService {
  private baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

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
}
