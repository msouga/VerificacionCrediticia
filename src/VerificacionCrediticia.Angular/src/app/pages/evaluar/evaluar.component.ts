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
import { DecimalPipe, PercentPipe, KeyValuePipe } from '@angular/common';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import { LoggingService } from '../../services/logging.service';
import { ResultadoEvaluacion } from '../../models/resultado-evaluacion.model';
import { DocumentoIdentidad } from '../../models/documento-identidad.model';
import { VigenciaPoder } from '../../models/vigencia-poder.model';
import { Recomendacion, Severidad } from '../../models/enums';
import { GrafoRedComponent } from '../../components/grafo-red/grafo-red.component';

@Component({
  selector: 'app-evaluar',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatCheckboxModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatProgressBarModule, MatExpansionModule,
    MatChipsModule, DecimalPipe, PercentPipe, KeyValuePipe, GrafoRedComponent
  ],
  templateUrl: './evaluar.component.html',
  styleUrl: './evaluar.component.scss'
})
export class EvaluarComponent {
  form: FormGroup;
  loading = false;
  resultado: ResultadoEvaluacion | null = null;
  error: string | null = null;

  // DNI upload
  archivoSeleccionado: File | null = null;
  loadingDni = false;
  dniResultado: DocumentoIdentidad | null = null;
  dniError: string | null = null;
  dragOver = false;

  // Vigencia de Poder upload
  archivoVigencia: File | null = null;
  loadingVigencia = false;
  vigenciaPoder: VigenciaPoder | null = null;
  vigenciaError: string | null = null;
  dragOverVigencia = false;
  vigenciaProgreso: string[] = [];

  private extensionesPermitidas = ['.pdf', '.jpg', '.jpeg', '.png', '.bmp', '.tiff'];
  private tamanoMaximoMb = 4;

  profundidades = [
    { value: 1, label: 'Nivel 1 - Solo relaciones directas' },
    { value: 2, label: 'Nivel 2 - Dos niveles de profundidad' },
    { value: 3, label: 'Nivel 3 - Analisis completo' }
  ];

  constructor(
    private fb: FormBuilder,
    private api: VerificacionApiService,
    private log: LoggingService
  ) {
    this.form = this.fb.group({
      dniSolicitante: ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
      rucEmpresa: ['', [Validators.required, Validators.pattern(/^\d{11}$/)]],
      profundidadMaxima: [2],
      incluirDetalleGrafo: [true]
    });
  }

  // DNI Upload
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.validarYAsignar(input.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.validarYAsignar(event.dataTransfer.files[0]);
    }
  }

  private validarYAsignar(archivo: File): void {
    this.dniError = null;
    const extension = '.' + archivo.name.split('.').pop()?.toLowerCase();
    if (!this.extensionesPermitidas.includes(extension)) {
      this.dniError = `Formato no soportado. Permitidos: ${this.extensionesPermitidas.join(', ')}`;
      return;
    }
    if (archivo.size > this.tamanoMaximoMb * 1024 * 1024) {
      this.dniError = `El archivo excede el limite de ${this.tamanoMaximoMb} MB`;
      return;
    }
    this.archivoSeleccionado = archivo;
    this.dniResultado = null;
  }

  procesarDni(): void {
    if (!this.archivoSeleccionado) return;

    this.loadingDni = true;
    this.dniError = null;
    this.dniResultado = null;

    this.log.info('Procesando DNI', 'EvaluarComponent', { archivo: this.archivoSeleccionado.name });

    this.api.procesarDni(this.archivoSeleccionado).subscribe({
      next: (res) => {
        this.dniResultado = res;
        this.loadingDni = false;
        this.log.info('DNI procesado', 'EvaluarComponent', {
          dni: res.numeroDocumento || '',
          validado: res.dniValidado ?? false,
          confianza: res.confianzaPromedio
        });
        if (res.dniValidado === true && res.numeroDocumento) {
          this.form.patchValue({ dniSolicitante: res.numeroDocumento });
        }
      },
      error: (err) => {
        this.dniError = err.error?.detail || err.error?.message || err.message || 'Error al procesar el documento';
        this.loadingDni = false;
        this.log.error('Error al procesar DNI', 'EvaluarComponent', { error: this.dniError }, err);
      }
    });
  }

  limpiarDni(): void {
    this.archivoSeleccionado = null;
    this.dniResultado = null;
    this.dniError = null;
  }

  getNombreCompleto(): string {
    if (!this.dniResultado) return '';
    const nombres = this.dniResultado.nombres || '';
    const apellidos = this.dniResultado.apellidos || '';
    return `${nombres} ${apellidos}`.trim() || 'No disponible';
  }

  getConfianzaColor(valor: number): string {
    if (valor >= 0.8) return 'success';
    if (valor >= 0.5) return 'warning';
    return 'error';
  }

  getConfianzaLabel(key: string): string {
    const labels: Record<string, string> = {
      nombres: 'Nombres', apellidos: 'Apellidos',
      numeroDocumento: 'Nro. Documento', fechaNacimiento: 'Fecha Nac.',
      fechaExpiracion: 'Fecha Exp.', sexo: 'Sexo',
      estadoCivil: 'Estado Civil', direccion: 'Direccion'
    };
    return labels[key] || key;
  }

  // Vigencia de Poder Upload
  onFileSelectedVigencia(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.validarYAsignarVigencia(input.files[0]);
    }
  }

  onDragOverVigencia(event: DragEvent): void {
    event.preventDefault();
    this.dragOverVigencia = true;
  }

  onDragLeaveVigencia(event: DragEvent): void {
    event.preventDefault();
    this.dragOverVigencia = false;
  }

  onDropVigencia(event: DragEvent): void {
    event.preventDefault();
    this.dragOverVigencia = false;
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.validarYAsignarVigencia(event.dataTransfer.files[0]);
    }
  }

  private validarYAsignarVigencia(archivo: File): void {
    this.vigenciaError = null;
    const extension = '.' + archivo.name.split('.').pop()?.toLowerCase();
    if (!this.extensionesPermitidas.includes(extension)) {
      this.vigenciaError = `Formato no soportado. Permitidos: ${this.extensionesPermitidas.join(', ')}`;
      return;
    }
    if (archivo.size > this.tamanoMaximoMb * 1024 * 1024) {
      this.vigenciaError = `El archivo excede el limite de ${this.tamanoMaximoMb} MB`;
      return;
    }
    this.archivoVigencia = archivo;
    this.vigenciaPoder = null;
  }

  async procesarVigenciaPoder(): Promise<void> {
    if (!this.archivoVigencia) return;

    this.loadingVigencia = true;
    this.vigenciaError = null;
    this.vigenciaPoder = null;
    this.vigenciaProgreso = [];

    this.log.info('Procesando Vigencia de Poder', 'EvaluarComponent', { archivo: this.archivoVigencia.name });

    try {
      const res = await this.api.procesarVigenciaPoder(
        this.archivoVigencia,
        (mensaje: string) => {
          this.vigenciaProgreso = [...this.vigenciaProgreso, mensaje];
        }
      );

      this.vigenciaPoder = res;
      this.loadingVigencia = false;
      this.log.info('Vigencia de Poder procesada', 'EvaluarComponent', {
        ruc: res.ruc || '',
        empresa: res.razonSocial || '',
        representantes: res.representantes?.length || 0,
        confianza: res.confianzaPromedio
      });
      // Auto-llenar RUC si fue validado
      if (res.rucValidado === true && res.ruc) {
        this.form.patchValue({ rucEmpresa: res.ruc });
      }
    } catch (err: any) {
      this.vigenciaError = err.message || 'Error al procesar el documento';
      this.loadingVigencia = false;
      this.log.error('Error al procesar Vigencia de Poder', 'EvaluarComponent', { error: this.vigenciaError }, err);
    }
  }

  limpiarVigencia(): void {
    this.archivoVigencia = null;
    this.vigenciaPoder = null;
    this.vigenciaError = null;
  }

  // Evaluacion
  evaluar(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.resultado = null;
    this.error = null;

    this.log.info('Iniciando evaluacion', 'EvaluarComponent', this.form.value);

    this.api.evaluar(this.form.value).subscribe({
      next: (res) => {
        this.resultado = res;
        this.loading = false;
        this.log.info('Evaluacion completada', 'EvaluarComponent', {
          score: res.scoreFinal,
          recomendacion: res.recomendacion
        });
      },
      error: (err) => {
        this.error = err.error?.message || err.message || 'Error al evaluar';
        this.loading = false;
        this.log.error('Error en evaluacion', 'EvaluarComponent', { error: this.error }, err);
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
