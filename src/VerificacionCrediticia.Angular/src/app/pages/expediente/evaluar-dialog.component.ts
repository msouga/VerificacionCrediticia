import { Component, Inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import { Expediente, ProgresoEvaluacion } from '../../models/expediente.model';

export interface EvaluarDialogData {
  expedienteId: number;
}

export interface EvaluarDialogResult {
  expediente?: Expediente;
  cancelado?: boolean;
  error?: string;
}

@Component({
  selector: 'app-evaluar-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatProgressBarModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Evaluando expediente</h2>
    <mat-dialog-content>
      <div class="evaluar-progress">
        <mat-progress-bar
          [mode]="completado ? 'determinate' : 'determinate'"
          [value]="porcentaje">
        </mat-progress-bar>

        @if (archivoActual) {
          <p class="archivo-actual">Procesando: {{ archivoActual }}</p>
        }
        @if (pasoActual) {
          <p class="paso-actual">{{ pasoActual }}</p>
        }
        @if (totalDocumentos > 0) {
          <p class="doc-count">Documento {{ documentoActual }} de {{ totalDocumentos }}</p>
        }

        @if (errorMsg) {
          <div class="error-container">
            <mat-icon>error</mat-icon>
            <span>{{ errorMsg }}</span>
          </div>
        }

        @if (completado && !errorMsg) {
          <div class="success-container">
            <mat-icon>check_circle</mat-icon>
            <span>Evaluacion completada</span>
          </div>
        }
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @if (!completado && !errorMsg) {
        <button mat-button (click)="cancelar()">Cancelar</button>
      }
      @if (errorMsg) {
        <button mat-button (click)="cerrar()">Cerrar</button>
      }
    </mat-dialog-actions>
  `,
  styles: [`
    .evaluar-progress {
      min-width: 400px;
      padding: 16px 0;
    }
    .archivo-actual {
      margin: 12px 0 4px;
      font-weight: 500;
      font-size: 0.95rem;
    }
    .paso-actual {
      margin: 4px 0;
      font-size: 0.85rem;
      color: rgba(0, 0, 0, 0.6);
    }
    .doc-count {
      margin: 8px 0 0;
      font-size: 0.85rem;
      color: rgba(0, 0, 0, 0.5);
    }
    .error-container {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 12px;
      padding: 8px;
      background: #ffebee;
      border-radius: 8px;
      color: #d32f2f;
    }
    .success-container {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 12px;
      padding: 8px;
      background: #e8f5e9;
      border-radius: 8px;
      color: #2e7d32;
    }
  `]
})
export class EvaluarDialogComponent {
  archivoActual = '';
  pasoActual = '';
  documentoActual = 0;
  totalDocumentos = 0;
  porcentaje = 0;
  completado = false;
  errorMsg = '';

  private abortController = new AbortController();

  constructor(
    private dialogRef: MatDialogRef<EvaluarDialogComponent, EvaluarDialogResult>,
    @Inject(MAT_DIALOG_DATA) private data: EvaluarDialogData,
    private api: VerificacionApiService,
    private cdr: ChangeDetectorRef
  ) {
    this.dialogRef.disableClose = true;
    this.iniciarEvaluacion();
  }

  private async iniciarEvaluacion(): Promise<void> {
    try {
      await this.api.evaluarExpedienteSSE(
        this.data.expedienteId,
        {
          onProgress: (progreso: ProgresoEvaluacion) => {
            this.archivoActual = progreso.archivo;
            this.pasoActual = progreso.paso;
            this.documentoActual = progreso.documentoActual;
            this.totalDocumentos = progreso.totalDocumentos;
            this.porcentaje = this.totalDocumentos > 0
              ? Math.round((this.documentoActual / this.totalDocumentos) * 100)
              : 0;
            this.cdr.markForCheck();
          },
          onResult: (expediente: Expediente) => {
            this.completado = true;
            this.porcentaje = 100;
            this.pasoActual = '';
            this.cdr.markForCheck();
            // Cerrar el dialog despues de un breve delay para que se vea el 100%
            setTimeout(() => {
              this.dialogRef.close({ expediente });
            }, 500);
          },
          onError: (error: string) => {
            this.errorMsg = error;
            this.cdr.markForCheck();
          }
        },
        this.abortController.signal
      );
    } catch (err: unknown) {
      if (this.abortController.signal.aborted) return;
      const message = err instanceof Error ? err.message : 'Error desconocido';
      this.errorMsg = message;
      this.cdr.markForCheck();
    }
  }

  cancelar(): void {
    this.abortController.abort();
    this.dialogRef.close({ cancelado: true });
  }

  cerrar(): void {
    this.dialogRef.close({ error: this.errorMsg });
  }
}
