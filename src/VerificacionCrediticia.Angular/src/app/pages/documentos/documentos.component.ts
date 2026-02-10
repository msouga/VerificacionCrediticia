import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DecimalPipe, PercentPipe, KeyValuePipe } from '@angular/common';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import { DocumentoIdentidad } from '../../models/documento-identidad.model';

@Component({
  selector: 'app-documentos',
  standalone: true,
  imports: [
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatProgressBarModule,
    DecimalPipe, PercentPipe, KeyValuePipe
  ],
  templateUrl: './documentos.component.html',
  styleUrl: './documentos.component.scss'
})
export class DocumentosComponent {
  archivoSeleccionado: File | null = null;
  loading = false;
  resultado: DocumentoIdentidad | null = null;
  error: string | null = null;
  dragOver = false;

  private extensionesPermitidas = ['.pdf', '.jpg', '.jpeg', '.png', '.bmp', '.tiff'];
  private tamanoMaximoMb = 4;

  constructor(private api: VerificacionApiService) {}

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
    this.error = null;

    const extension = '.' + archivo.name.split('.').pop()?.toLowerCase();
    if (!this.extensionesPermitidas.includes(extension)) {
      this.error = `Formato no soportado. Formatos permitidos: ${this.extensionesPermitidas.join(', ')}`;
      return;
    }

    if (archivo.size > this.tamanoMaximoMb * 1024 * 1024) {
      this.error = `El archivo excede el limite de ${this.tamanoMaximoMb} MB`;
      return;
    }

    this.archivoSeleccionado = archivo;
    this.resultado = null;
  }

  procesar(): void {
    if (!this.archivoSeleccionado) return;

    this.loading = true;
    this.error = null;
    this.resultado = null;

    this.api.procesarDni(this.archivoSeleccionado).subscribe({
      next: (res) => {
        this.resultado = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.detail || err.error?.message || err.message || 'Error al procesar el documento';
        this.loading = false;
      }
    });
  }

  limpiar(): void {
    this.archivoSeleccionado = null;
    this.resultado = null;
    this.error = null;
  }

  getNombreCompleto(): string {
    if (!this.resultado) return '';
    const nombres = this.resultado.nombres || '';
    const apellidos = this.resultado.apellidos || '';
    return `${nombres} ${apellidos}`.trim() || 'No disponible';
  }

  getConfianzaColor(valor: number): string {
    if (valor >= 0.8) return 'success';
    if (valor >= 0.5) return 'warning';
    return 'error';
  }

  getConfianzaLabel(key: string): string {
    const labels: Record<string, string> = {
      nombres: 'Nombres',
      apellidos: 'Apellidos',
      numeroDocumento: 'Nro. Documento',
      fechaNacimiento: 'Fecha Nac.',
      fechaExpiracion: 'Fecha Exp.',
      sexo: 'Sexo',
      estadoCivil: 'Estado Civil',
      direccion: 'Direccion'
    };
    return labels[key] || key;
  }
}
