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
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import {
  TipoDocumentoConfig, ActualizarTipoDocumentoRequest,
  ReglaEvaluacionConfig, ActualizarReglaRequest
} from '../../models/configuracion.model';
import { NuevaReglaDialogComponent } from './nueva-regla-dialog.component';

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
    MatCardModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDialogModule
  ],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfiguracionComponent implements OnInit {
  // Tipos de documento
  tipos: TipoDocumentoConfig[] = [];
  displayedColumns = ['nombre', 'codigo', 'analyzerId', 'obligatorio', 'activo', 'orden'];
  cargando = true;
  actualizandoId: number | null = null;

  // Reglas de evaluacion
  reglas: ReglaEvaluacionConfig[] = [];
  reglasColumns = ['nombre', 'campo', 'operador', 'valor', 'peso', 'resultado', 'activa', 'orden', 'acciones'];
  cargandoReglas = true;
  actualizandoReglaId: number | null = null;

  operadores = [
    { valor: 0, simbolo: '>' },
    { valor: 1, simbolo: '<' },
    { valor: 2, simbolo: '>=' },
    { valor: 3, simbolo: '<=' },
    { valor: 4, simbolo: '==' },
    { valor: 5, simbolo: '!=' }
  ];

  resultados = [
    { valor: 0, nombre: 'Aprobar', color: 'aprobar' },
    { valor: 1, nombre: 'Rechazar', color: 'rechazar' },
    { valor: 2, nombre: 'Revisar', color: 'revisar' }
  ];

  constructor(
    private api: VerificacionApiService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.cargarTipos();
    this.cargarReglas();
  }

  // --- Tipos de documento ---

  cargarTipos(): void {
    this.cargando = true;
    this.api.getTiposDocumentoConfig().subscribe({
      next: (tipos) => {
        this.tipos = tipos;
        this.cargando = false;
        this.cdr.markForCheck();
      },
      error: () => {
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

  // --- Reglas de evaluacion ---

  cargarReglas(): void {
    this.cargandoReglas = true;
    this.api.getReglas().subscribe({
      next: (reglas) => {
        this.reglas = reglas;
        this.cargandoReglas = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.snackBar.open('Error al cargar reglas de evaluacion', 'Cerrar', { duration: 5000 });
        this.cargandoReglas = false;
        this.cdr.markForCheck();
      }
    });
  }

  getOperadorSimbolo(operador: number): string {
    return this.operadores.find(o => o.valor === operador)?.simbolo ?? '?';
  }

  getResultadoNombre(resultado: number): string {
    return this.resultados.find(r => r.valor === resultado)?.nombre ?? '?';
  }

  getResultadoColor(resultado: number): string {
    return this.resultados.find(r => r.valor === resultado)?.color ?? '';
  }

  actualizarRegla(regla: ReglaEvaluacionConfig): void {
    this.actualizandoReglaId = regla.id;
    this.cdr.markForCheck();

    const request: ActualizarReglaRequest = {
      descripcion: regla.descripcion,
      operador: regla.operador,
      valor: regla.valor,
      peso: regla.peso,
      resultado: regla.resultado,
      activa: regla.activa,
      orden: regla.orden
    };

    this.api.actualizarRegla(regla.id, request).subscribe({
      next: (actualizada) => {
        const index = this.reglas.findIndex(r => r.id === regla.id);
        if (index >= 0) {
          this.reglas[index] = actualizada;
        }
        this.actualizandoReglaId = null;
        this.snackBar.open(`${regla.nombre} actualizada`, 'OK', { duration: 2000 });
        this.cdr.markForCheck();
      },
      error: () => {
        this.actualizandoReglaId = null;
        this.snackBar.open(`Error al actualizar ${regla.nombre}`, 'Cerrar', { duration: 5000 });
        this.cargarReglas();
      }
    });
  }

  onReglaValorBlur(regla: ReglaEvaluacionConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoValor = parseFloat(input.value);
    if (!isNaN(nuevoValor) && nuevoValor !== regla.valor) {
      regla.valor = nuevoValor;
      this.actualizarRegla(regla);
    }
  }

  onReglaPesoBlur(regla: ReglaEvaluacionConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoPeso = parseFloat(input.value);
    if (!isNaN(nuevoPeso) && nuevoPeso !== regla.peso) {
      regla.peso = nuevoPeso;
      this.actualizarRegla(regla);
    }
  }

  onReglaOrdenBlur(regla: ReglaEvaluacionConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoOrden = parseInt(input.value, 10);
    if (!isNaN(nuevoOrden) && nuevoOrden !== regla.orden) {
      regla.orden = nuevoOrden;
      this.actualizarRegla(regla);
    }
  }

  onOperadorChange(regla: ReglaEvaluacionConfig, nuevoOperador: number): void {
    if (nuevoOperador !== regla.operador) {
      regla.operador = nuevoOperador;
      this.actualizarRegla(regla);
    }
  }

  onResultadoChange(regla: ReglaEvaluacionConfig, nuevoResultado: number): void {
    if (nuevoResultado !== regla.resultado) {
      regla.resultado = nuevoResultado;
      this.actualizarRegla(regla);
    }
  }

  toggleReglaActiva(regla: ReglaEvaluacionConfig, activa: boolean): void {
    regla.activa = activa;
    this.actualizarRegla(regla);
  }

  abrirNuevaRegla(): void {
    const dialogRef = this.dialog.open(NuevaReglaDialogComponent, {
      width: '560px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.crearRegla(result).subscribe({
          next: (nueva) => {
            this.reglas = [...this.reglas, nueva];
            this.snackBar.open(`Regla "${nueva.nombre}" creada`, 'OK', { duration: 3000 });
            this.cdr.markForCheck();
          },
          error: () => {
            this.snackBar.open('Error al crear regla', 'Cerrar', { duration: 5000 });
          }
        });
      }
    });
  }

  eliminarRegla(regla: ReglaEvaluacionConfig): void {
    if (!confirm(`¿Eliminar la regla "${regla.nombre}"?`)) return;

    this.api.eliminarRegla(regla.id).subscribe({
      next: () => {
        this.reglas = this.reglas.filter(r => r.id !== regla.id);
        this.snackBar.open(`Regla "${regla.nombre}" eliminada`, 'OK', { duration: 3000 });
        this.cdr.markForCheck();
      },
      error: () => {
        this.snackBar.open(`Error al eliminar ${regla.nombre}`, 'Cerrar', { duration: 5000 });
      }
    });
  }
}
