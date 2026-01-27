import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { SolicitudVerificacion } from '../models/solicitud-verificacion.model';
import { ResultadoEvaluacion } from '../models/resultado-evaluacion.model';
import { Persona } from '../models/persona.model';
import { Empresa } from '../models/empresa.model';

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

  consultarPersona(dni: string): Observable<Persona> {
    return this.http.get<Persona>(
      `${this.baseUrl}/api/verificacion/persona/${dni}`
    );
  }

  consultarEmpresa(ruc: string): Observable<Empresa> {
    return this.http.get<Empresa>(
      `${this.baseUrl}/api/verificacion/empresa/${ruc}`
    );
  }
}
