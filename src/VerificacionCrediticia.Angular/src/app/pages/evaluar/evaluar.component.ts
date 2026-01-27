import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { DecimalPipe } from '@angular/common';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import { ResultadoEvaluacion } from '../../models/resultado-evaluacion.model';
import { Recomendacion, Severidad } from '../../models/enums';
import { GrafoRedComponent } from '../../components/grafo-red/grafo-red.component';

@Component({
  selector: 'app-evaluar',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatCheckboxModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatProgressBarModule, MatExpansionModule,
    MatChipsModule, DecimalPipe, GrafoRedComponent
  ],
  templateUrl: './evaluar.component.html',
  styleUrl: './evaluar.component.scss'
})
export class EvaluarComponent {
  form: FormGroup;
  loading = false;
  resultado: ResultadoEvaluacion | null = null;
  error: string | null = null;

  profundidades = [
    { value: 1, label: 'Nivel 1 - Solo relaciones directas' },
    { value: 2, label: 'Nivel 2 - Dos niveles de profundidad' },
    { value: 3, label: 'Nivel 3 - Analisis completo' }
  ];

  constructor(
    private fb: FormBuilder,
    private api: VerificacionApiService
  ) {
    this.form = this.fb.group({
      dniSolicitante: ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
      rucEmpresa: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
      profundidadMaxima: [2],
      incluirDetalleGrafo: [true]
    });
  }

  evaluar(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.resultado = null;
    this.error = null;

    this.api.evaluar(this.form.value).subscribe({
      next: (res) => {
        this.resultado = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || err.message || 'Error al evaluar';
        this.loading = false;
      }
    });
  }

  getHeaderClass(): string {
    if (!this.resultado) return '';
    const r = this.resultado.recomendacion;
    if (r === Recomendacion.Aprobar || r === 0 as any) return 'header-success';
    if (r === Recomendacion.RevisarManualmente || r === 1 as any) return 'header-warning';
    if (r === Recomendacion.Rechazar || r === 2 as any) return 'header-error';
    return '';
  }

  getRecomendacionText(): string {
    if (!this.resultado) return '';
    const r = this.resultado.recomendacion;
    if (r === Recomendacion.Aprobar || r === 0 as any) return 'APROBAR';
    if (r === Recomendacion.RevisarManualmente || r === 1 as any) return 'REVISAR MANUALMENTE';
    if (r === Recomendacion.Rechazar || r === 2 as any) return 'RECHAZAR';
    return '';
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'success';
    if (score >= 60) return 'warning';
    return 'error';
  }

  getStatusLabel(score: number): string {
    if (score >= 80) return 'OK';
    if (score >= 60) return 'ALERTA';
    return 'CRITICO';
  }

  getSeveridadColor(severidad: any): string {
    if (severidad === Severidad.Critica || severidad === 3) return 'error';
    if (severidad === Severidad.Alta || severidad === 2) return 'warning';
    if (severidad === Severidad.Media || severidad === 1) return 'info';
    return 'default';
  }

  get desglose() { return this.resultado?.desgloseScore; }
  get resumen() { return this.resultado?.resumen; }

  get motivosSolicitanteLimit(): string[] {
    return this.desglose?.motivosSolicitante || [];
  }

  get motivosEmpresaLimit(): string[] {
    return this.desglose?.motivosEmpresa || [];
  }

  get motivosRelacionesLimit(): string[] {
    const motivos = this.desglose?.motivosRelaciones || [];
    return motivos.slice(0, 8);
  }

  get motivosRelacionesExtra(): number {
    const total = this.desglose?.motivosRelaciones?.length || 0;
    return total > 8 ? total - 8 : 0;
  }
}
