import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DecimalPipe, PercentPipe, DatePipe } from '@angular/common';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import {
  Expediente, TipoDocumento, DocumentoProcesadoResumen,
  EstadoExpediente, EstadoDocumento, ResultadoRegla
} from '../../models/expediente.model';

interface DocumentoSlot {
  tipo: TipoDocumento;
  documento: DocumentoProcesadoResumen | null;
  archivo: File | null;
  loading: boolean;
  progreso: string[];
  error: string | null;
  dragOver: boolean;
}

@Component({
  selector: 'app-expediente',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatProgressBarModule,
    MatExpansionModule, MatChipsModule, MatTooltipModule,
    DecimalPipe, PercentPipe, DatePipe
  ],
  templateUrl: './expediente.component.html',
  styleUrl: './expediente.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExpedienteComponent implements OnInit {
  form: FormGroup;
  expediente: Expediente | null = null;
  loading = false;
  creando = false;
  evaluando = false;
  error: string | null = null;

  documentoSlots: DocumentoSlot[] = [];

  private extensionesPermitidas = ['.pdf', '.jpg', '.jpeg', '.png', '.bmp', '.tiff'];
  private tamanoMaximoMb = 4;

  constructor(
    private fb: FormBuilder,
    private api: VerificacionApiService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      dniSolicitante: ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
      rucEmpresa: ['', [Validators.pattern(/^\d{11}$/)]]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.cargarExpediente(+id);
    }
  }

  crearExpediente(): void {
    if (this.form.invalid) return;

    this.creando = true;
    this.error = null;

    const request = {
      dniSolicitante: this.form.value.dniSolicitante,
      rucEmpresa: this.form.value.rucEmpresa || undefined
    };

    this.api.crearExpediente(request).subscribe({
      next: (exp) => {
        this.expediente = exp;
        this.creando = false;
        this.buildSlots();
        this.cdr.markForCheck();
        this.router.navigate(['/expediente', exp.id], { replaceUrl: true });
      },
      error: (err) => {
        this.error = err.error?.detail || err.message || 'Error al crear expediente';
        this.creando = false;
        this.cdr.markForCheck();
      }
    });
  }

  private cargarExpediente(id: number): void {
    this.loading = true;
    this.api.getExpediente(id).subscribe({
      next: (exp) => {
        this.expediente = exp;
        this.loading = false;
        this.buildSlots();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = err.error?.detail || 'Expediente no encontrado';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private recargarExpediente(): void {
    if (!this.expediente) return;
    this.api.getExpediente(this.expediente.id).subscribe({
      next: (exp) => {
        this.expediente = exp;
        this.buildSlots();
        this.cdr.markForCheck();
      }
    });
  }

  private buildSlots(): void {
    if (!this.expediente) return;
    this.documentoSlots = this.expediente.tiposDocumentoRequeridos
      .sort((a, b) => a.orden - b.orden)
      .map(tipo => {
        const doc = this.expediente!.documentos.find(
          d => d.codigoTipoDocumento === tipo.codigo
        ) || null;
        return {
          tipo,
          documento: doc,
          archivo: null,
          loading: false,
          progreso: [],
          error: null,
          dragOver: false
        };
      });
  }

  // Drag & Drop handlers
  onDragOver(event: DragEvent, slot: DocumentoSlot): void {
    event.preventDefault();
    slot.dragOver = true;
    this.cdr.markForCheck();
  }

  onDragLeave(event: DragEvent, slot: DocumentoSlot): void {
    event.preventDefault();
    slot.dragOver = false;
    this.cdr.markForCheck();
  }

  onDrop(event: DragEvent, slot: DocumentoSlot): void {
    event.preventDefault();
    slot.dragOver = false;
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.seleccionarArchivo(event.dataTransfer.files[0], slot);
    }
  }

  onFileSelected(event: Event, slot: DocumentoSlot): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.seleccionarArchivo(input.files[0], slot);
    }
  }

  private seleccionarArchivo(archivo: File, slot: DocumentoSlot): void {
    slot.error = null;
    const extension = '.' + archivo.name.split('.').pop()?.toLowerCase();
    if (!this.extensionesPermitidas.includes(extension)) {
      slot.error = `Formato no soportado. Permitidos: ${this.extensionesPermitidas.join(', ')}`;
      this.cdr.markForCheck();
      return;
    }
    if (archivo.size > this.tamanoMaximoMb * 1024 * 1024) {
      slot.error = `El archivo excede el limite de ${this.tamanoMaximoMb} MB`;
      this.cdr.markForCheck();
      return;
    }
    slot.archivo = archivo;
    this.cdr.markForCheck();
    this.procesarDocumento(slot);
  }

  async procesarDocumento(slot: DocumentoSlot): Promise<void> {
    if (!slot.archivo || !this.expediente) return;

    slot.loading = true;
    slot.error = null;
    slot.progreso = [];
    this.cdr.markForCheck();

    try {
      if (slot.documento) {
        await this.api.reemplazarDocumentoExpediente(
          this.expediente.id,
          slot.documento.id,
          slot.archivo,
          (msg: string) => {
            slot.progreso = [...slot.progreso, msg];
            this.cdr.markForCheck();
          }
        );
      } else {
        await this.api.procesarDocumentoExpediente(
          this.expediente.id,
          slot.tipo.codigo,
          slot.archivo,
          (msg: string) => {
            slot.progreso = [...slot.progreso, msg];
            this.cdr.markForCheck();
          }
        );
      }

      slot.loading = false;
      slot.archivo = null;
      this.cdr.markForCheck();
      this.recargarExpediente();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error al procesar';
      slot.error = message;
      slot.loading = false;
      slot.archivo = null;
      this.cdr.markForCheck();
    }
  }

  evaluar(): void {
    if (!this.expediente || !this.expediente.puedeEvaluar) return;

    this.evaluando = true;
    this.error = null;
    this.cdr.markForCheck();

    this.api.evaluarExpediente(this.expediente.id).subscribe({
      next: (exp) => {
        this.expediente = exp;
        this.evaluando = false;
        this.buildSlots();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = err.error?.detail || err.message || 'Error al evaluar';
        this.evaluando = false;
        this.cdr.markForCheck();
      }
    });
  }

  nuevoExpediente(): void {
    this.expediente = null;
    this.documentoSlots = [];
    this.error = null;
    this.form.reset();
    this.router.navigate(['/expediente'], { replaceUrl: true });
  }

  // Helpers de estado
  getEstadoLabel(estado: EstadoExpediente): string {
    const labels: Record<number, string> = {
      [EstadoExpediente.Iniciado]: 'Iniciado',
      [EstadoExpediente.EnProceso]: 'En Proceso',
      [EstadoExpediente.DocumentosCompletos]: 'Documentos Completos',
      [EstadoExpediente.Evaluado]: 'Evaluado'
    };
    return labels[estado] || 'Desconocido';
  }

  getEstadoColor(estado: EstadoExpediente): string {
    const colors: Record<number, string> = {
      [EstadoExpediente.Iniciado]: 'default',
      [EstadoExpediente.EnProceso]: 'accent',
      [EstadoExpediente.DocumentosCompletos]: 'primary',
      [EstadoExpediente.Evaluado]: 'primary'
    };
    return colors[estado] || 'default';
  }

  getDocEstadoIcon(estado: EstadoDocumento): string {
    const icons: Record<number, string> = {
      [EstadoDocumento.Pendiente]: 'hourglass_empty',
      [EstadoDocumento.Procesando]: 'sync',
      [EstadoDocumento.Procesado]: 'check_circle',
      [EstadoDocumento.Error]: 'error'
    };
    return icons[estado] || 'help';
  }

  getDocEstadoClass(estado: EstadoDocumento): string {
    const classes: Record<number, string> = {
      [EstadoDocumento.Pendiente]: 'estado-pendiente',
      [EstadoDocumento.Procesando]: 'estado-procesando',
      [EstadoDocumento.Procesado]: 'estado-procesado',
      [EstadoDocumento.Error]: 'estado-error'
    };
    return classes[estado] || '';
  }

  getRecomendacionLabel(valor: number): string {
    const labels: Record<number, string> = { 0: 'APROBAR', 1: 'REVISAR MANUALMENTE', 2: 'RECHAZAR' };
    return labels[valor] || 'DESCONOCIDO';
  }

  getRecomendacionClass(valor: number): string {
    const classes: Record<number, string> = { 0: 'rec-aprobar', 1: 'rec-revisar', 2: 'rec-rechazar' };
    return classes[valor] || '';
  }

  getNivelRiesgoLabel(valor: number): string {
    const labels: Record<number, string> = { 0: 'Muy Bajo', 1: 'Bajo', 2: 'Moderado', 3: 'Alto', 4: 'Muy Alto' };
    return labels[valor] || 'Desconocido';
  }

  getReglaResultadoClass(resultado: ResultadoRegla): string {
    const classes: Record<number, string> = {
      [ResultadoRegla.Aprobar]: 'regla-aprobar',
      [ResultadoRegla.Rechazar]: 'regla-rechazar',
      [ResultadoRegla.Revisar]: 'regla-revisar'
    };
    return classes[resultado] || '';
  }

  get progreso(): number {
    if (!this.expediente || this.expediente.totalDocumentosObligatorios === 0) return 0;
    return (this.expediente.documentosObligatoriosCompletos / this.expediente.totalDocumentosObligatorios) * 100;
  }

  get nombreCompleto(): string {
    if (!this.expediente) return '';
    const n = this.expediente.nombresSolicitante || '';
    const a = this.expediente.apellidosSolicitante || '';
    return `${n} ${a}`.trim();
  }
}
