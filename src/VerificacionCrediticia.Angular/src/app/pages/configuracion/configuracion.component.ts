import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import { TipoDocumentoConfig, ActualizarTipoDocumentoRequest } from '../../models/configuracion.model';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatSlideToggleModule,
    MatInputModule,
    MatFormFieldModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfiguracionComponent implements OnInit {
  tipos: TipoDocumentoConfig[] = [];
  displayedColumns = ['nombre', 'codigo', 'analyzerId', 'obligatorio', 'activo', 'orden'];
  cargando = true;
  actualizandoId: number | null = null;

  constructor(
    private api: VerificacionApiService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarTipos();
  }

  cargarTipos(): void {
    this.cargando = true;
    this.api.getTiposDocumentoConfig().subscribe({
      next: (tipos) => {
        this.tipos = tipos;
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.snackBar.open('Error al cargar tipos de documento', 'Cerrar', { duration: 5000 });
        this.cargando = false;
        this.cdr.markForCheck();
      }
    });
  }

  actualizarTipo(tipo: TipoDocumentoConfig): void {
    this.actualizandoId = tipo.id;
    this.cdr.markForCheck();

    const request: ActualizarTipoDocumentoRequest = {
      esObligatorio: tipo.esObligatorio,
      activo: tipo.activo,
      orden: tipo.orden,
      descripcion: tipo.descripcion,
      analyzerId: tipo.analyzerId
    };

    this.api.actualizarTipoDocumento(tipo.id, request).subscribe({
      next: (actualizado) => {
        const index = this.tipos.findIndex(t => t.id === tipo.id);
        if (index >= 0) {
          this.tipos[index] = actualizado;
        }
        this.actualizandoId = null;
        this.snackBar.open(`${tipo.nombre} actualizado`, 'OK', { duration: 2000 });
        this.cdr.markForCheck();
      },
      error: () => {
        this.actualizandoId = null;
        this.snackBar.open(`Error al actualizar ${tipo.nombre}`, 'Cerrar', { duration: 5000 });
        this.cargarTipos();
      }
    });
  }

  onOrdenBlur(tipo: TipoDocumentoConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoOrden = parseInt(input.value, 10);
    if (!isNaN(nuevoOrden) && nuevoOrden !== tipo.orden) {
      tipo.orden = nuevoOrden;
      this.actualizarTipo(tipo);
    }
  }
}
