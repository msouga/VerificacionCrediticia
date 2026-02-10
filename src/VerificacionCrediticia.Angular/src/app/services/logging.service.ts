import { Injectable, ErrorHandler, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { environment } from '../environments/environment';

export enum LogLevel {
  Debug = 'Debug',
  Information = 'Information',
  Warning = 'Warning',
  Error = 'Error'
}

interface LogEntry {
  nivel: string;
  mensaje: string;
  origen: string;
  datos?: Record<string, unknown>;
  stackTrace?: string;
}

@Injectable({ providedIn: 'root' })
export class LoggingService {
  private http = inject(HttpClient);

  private appInsights: ApplicationInsights | null = null;
  private apiUrl = `${environment.apiBaseUrl}/api/logs`;

  constructor() {
    this.initAppInsights();
  }

  private initAppInsights(): void {
    const connStr = environment.appInsights?.connectionString;
    if (!connStr) return;

    this.appInsights = new ApplicationInsights({
      config: {
        connectionString: connStr,
        enableAutoRouteTracking: true,
        enableCorsCorrelation: true,
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true
      }
    });
    this.appInsights.loadAppInsights();
  }

  debug(mensaje: string, origen: string, datos?: Record<string, unknown>): void {
    this.log(LogLevel.Debug, mensaje, origen, datos);
  }

  info(mensaje: string, origen: string, datos?: Record<string, unknown>): void {
    this.log(LogLevel.Information, mensaje, origen, datos);
  }

  warn(mensaje: string, origen: string, datos?: Record<string, unknown>): void {
    this.log(LogLevel.Warning, mensaje, origen, datos);
  }

  error(mensaje: string, origen: string, datos?: Record<string, unknown>, error?: Error): void {
    this.log(LogLevel.Error, mensaje, origen, datos, error);

    if (this.appInsights && error) {
      this.appInsights.trackException({ exception: error });
    }
  }

  private log(
    nivel: LogLevel,
    mensaje: string,
    origen: string,
    datos?: Record<string, unknown>,
    err?: Error
  ): void {
    // Console en desarrollo
    if (!environment.production) {
      const consoleFn = nivel === LogLevel.Error ? console.error
        : nivel === LogLevel.Warning ? console.warn
        : console.log;
      consoleFn(`[${nivel}] ${origen}: ${mensaje}`, datos || '');
    }

    // App Insights (si esta configurado)
    if (this.appInsights) {
      this.appInsights.trackTrace({
        message: `${origen}: ${mensaje}`,
        severityLevel: this.toSeverityLevel(nivel)
      }, datos as Record<string, string>);
    }

    // Enviar al backend (solo Warning y Error para no saturar)
    if (nivel === LogLevel.Warning || nivel === LogLevel.Error) {
      const entry: LogEntry = { nivel, mensaje, origen, datos, stackTrace: err?.stack };
      this.http.post(this.apiUrl, entry).subscribe({
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        error: () => {} // Silenciar errores del log para evitar loops
      });
    }
  }

  private toSeverityLevel(nivel: LogLevel): number {
    switch (nivel) {
      case LogLevel.Debug: return 0;
      case LogLevel.Information: return 1;
      case LogLevel.Warning: return 2;
      case LogLevel.Error: return 3;
      default: return 1;
    }
  }
}

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private logging = inject(LoggingService);


  handleError(error: Error): void {
    this.logging.error(
      error.message || 'Error no manejado',
      'GlobalErrorHandler',
      {},
      error
    );
    console.error(error);
  }
}
