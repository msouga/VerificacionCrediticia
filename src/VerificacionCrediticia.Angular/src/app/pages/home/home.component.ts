import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { DatePipe, DecimalPipe } from '@angular/common';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import { EstadisticasExpedientes, ExpedienteEvaluadoResumen } from '../../models/expediente.model';
import { NuevoExpedienteDialogComponent } from '../expedientes/nuevo-expediente-dialog.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MatCardModule, MatButtonModule, MatIconModule,
    MatTableModule, MatProgressBarModule, MatProgressSpinnerModule,
    DatePipe, DecimalPipe
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private dialog = inject(MatDialog);
  private api = inject(VerificacionApiService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  today = new Date();
  cargando = true;

  stats: EstadisticasExpedientes = {
    totalExpedientes: 0,
    evaluados: 0,
    enProceso: 0,
    aprobados: 0,
    enRevision: 0,
    rechazados: 0,
    scorePromedio: 0,
    recientes: []
  };

  displayedColumns = ['descripcion', 'empresa', 'score', 'resultado'];

  ngOnInit(): void {
    this.cargarEstadisticas();
  }

  cargarEstadisticas(): void {
    this.cargando = true;
    this.api.getEstadisticas().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.cargando = false;
        this.cdr.markForCheck();
      }
    });
  }

  nuevoExpediente(): void {
    const dialogRef = this.dialog.open(NuevoExpedienteDialogComponent, {
      width: '400px'
    });

    dialogRef.afterClosed().subscribe((descripcion: string | undefined) => {
      if (descripcion) {
        this.api.crearExpediente({ descripcion }).subscribe({
          next: (exp) => {
            this.router.navigate(['/expediente', exp.id]);
          }
        });
      }
    });
  }

  verExpediente(exp: ExpedienteEvaluadoResumen): void {
    this.router.navigate(['/expediente', exp.id]);
  }

  get porcentajeAprobacion(): number {
    return this.stats.evaluados > 0
      ? Math.round((this.stats.aprobados / this.stats.evaluados) * 100)
      : 0;
  }

  get porcentajeRevision(): number {
    return this.stats.evaluados > 0
      ? Math.round((this.stats.enRevision / this.stats.evaluados) * 100)
      : 0;
  }

  get porcentajeRechazo(): number {
    return this.stats.evaluados > 0
      ? Math.round((this.stats.rechazados / this.stats.evaluados) * 100)
      : 0;
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'success';
    if (score >= 60) return 'warning';
    return 'error';
  }

  getResultadoTexto(recomendacion: number): string {
    switch (recomendacion) {
      case 0: return 'APROBADO';
      case 1: return 'REVISION';
      case 2: return 'RECHAZADO';
      default: return '?';
    }
  }

  getResultadoColor(recomendacion: number): string {
    switch (recomendacion) {
      case 0: return 'success';
      case 1: return 'warning';
      case 2: return 'error';
      default: return '';
    }
  }
}
