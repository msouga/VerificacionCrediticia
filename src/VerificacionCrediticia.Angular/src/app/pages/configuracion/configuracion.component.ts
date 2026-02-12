import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, inject, HostListener } from '@angular/core';
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
  ReglaEvaluacionConfig, ActualizarReglaRequest,
  ParametrosLineaCredito
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
  private api = inject(VerificacionApiService);
  private snackBar = inject(MatSnackBar);
  private cdr = inject(ChangeDetectorRef);
  private dialog = inject(MatDialog);

  // Tipos de documento
  tipos: TipoDocumentoConfig[] = [];
  tiposOriginales: TipoDocumentoConfig[] = [];
  displayedColumns = ['nombre', 'codigo', 'analyzerId', 'obligatorio', 'activo', 'orden'];
  cargando = true;
  guardandoTipos = false;

  // Reglas de evaluacion
  reglas: ReglaEvaluacionConfig[] = [];
  reglasOriginales: ReglaEvaluacionConfig[] = [];
  reglasColumns = ['nombre', 'campo', 'operador', 'valor', 'peso', 'resultado', 'activa', 'orden', 'acciones'];
  cargandoReglas = true;
  guardandoReglas = false;

  // Parametros de linea de credito
  parametrosLC: ParametrosLineaCredito = {
    porcentajeCapitalTrabajo: 20,
    porcentajePatrimonio: 30,
    porcentajeUtilidadNeta: 100,
    pesoRedNivel0: 100,
    pesoRedNivel1: 50,
    pesoRedNivel2: 25
  };
  parametrosLCOriginal: ParametrosLineaCredito = { ...this.parametrosLC };
  cargandoLC = true;
  guardandoLC = false;

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

  ngOnInit(): void {
    this.cargarTipos();
    this.cargarReglas();
    this.cargarParametrosLC();
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hayCambiosSinGuardar) {
      event.preventDefault();
    }
  }

  /** Verifica si el componente puede desactivarse (navegacion interna Angular) */
  canDeactivate(): boolean {
    if (!this.hayCambiosSinGuardar) return true;
    return confirm('Hay cambios sin guardar. ¿Desea salir y perder los cambios?');
  }

  get hayCambiosSinGuardar(): boolean {
    return this.hayTiposModificados || this.hayReglasModificadas || this.hayParametrosLCModificados;
  }

  // --- Reglas: dirty tracking ---

  get hayReglasModificadas(): boolean {
    if (this.reglas.length !== this.reglasOriginales.length) return false;
    return this.reglas.some((r, i) => {
      const o = this.reglasOriginales.find(orig => orig.id === r.id);
      if (!o) return true;
      return r.operador !== o.operador || r.valor !== o.valor || r.peso !== o.peso
        || r.resultado !== o.resultado || r.activa !== o.activa || r.orden !== o.orden
        || r.descripcion !== o.descripcion;
    });
  }

  getReglasModificadasIds(): Set<number> {
    const ids = new Set<number>();
    for (const r of this.reglas) {
      const o = this.reglasOriginales.find(orig => orig.id === r.id);
      if (!o) { ids.add(r.id); continue; }
      if (r.operador !== o.operador || r.valor !== o.valor || r.peso !== o.peso
        || r.resultado !== o.resultado || r.activa !== o.activa || r.orden !== o.orden
        || r.descripcion !== o.descripcion) {
        ids.add(r.id);
      }
    }
    return ids;
  }

  private clonarReglas(reglas: ReglaEvaluacionConfig[]): ReglaEvaluacionConfig[] {
    return reglas.map(r => ({ ...r }));
  }

  // --- Tipos de documento: dirty tracking ---

  get hayTiposModificados(): boolean {
    if (this.tipos.length !== this.tiposOriginales.length) return false;
    return this.tipos.some(t => {
      const o = this.tiposOriginales.find(orig => orig.id === t.id);
      if (!o) return true;
      return t.esObligatorio !== o.esObligatorio || t.activo !== o.activo || t.orden !== o.orden;
    });
  }

  isTipoModificado(tipo: TipoDocumentoConfig): boolean {
    const o = this.tiposOriginales.find(orig => orig.id === tipo.id);
    if (!o) return true;
    return tipo.esObligatorio !== o.esObligatorio || tipo.activo !== o.activo || tipo.orden !== o.orden;
  }

  private clonarTipos(tipos: TipoDocumentoConfig[]): TipoDocumentoConfig[] {
    return tipos.map(t => ({ ...t }));
  }

  cargarTipos(): void {
    this.cargando = true;
    this.api.getTiposDocumentoConfig().subscribe({
      next: (tipos) => {
        this.tipos = tipos;
        this.tiposOriginales = this.clonarTipos(tipos);
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

  onTipoToggleObligatorio(tipo: TipoDocumentoConfig, checked: boolean): void {
    tipo.esObligatorio = checked;
    this.cdr.markForCheck();
  }

  onTipoToggleActivo(tipo: TipoDocumentoConfig, checked: boolean): void {
    tipo.activo = checked;
    this.cdr.markForCheck();
  }

  onTipoOrdenChange(tipo: TipoDocumentoConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoOrden = parseInt(input.value, 10);
    if (!isNaN(nuevoOrden)) {
      tipo.orden = nuevoOrden;
      this.cdr.markForCheck();
    }
  }

  guardarTipos(): void {
    const modificados = this.tipos.filter(t => this.isTipoModificado(t));
    if (modificados.length === 0) return;

    this.guardandoTipos = true;
    this.cdr.markForCheck();

    let completadas = 0;
    let errores = 0;

    for (const tipo of modificados) {
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
          if (index >= 0) this.tipos[index] = actualizado;
          completadas++;
          this.verificarGuardadoTiposCompleto(completadas, errores, modificados.length);
        },
        error: () => {
          errores++;
          completadas++;
          this.verificarGuardadoTiposCompleto(completadas, errores, modificados.length);
        }
      });
    }
  }

  private verificarGuardadoTiposCompleto(completadas: number, errores: number, total: number): void {
    if (completadas < total) return;

    this.guardandoTipos = false;
    this.tiposOriginales = this.clonarTipos(this.tipos);

    if (errores > 0) {
      this.snackBar.open(`${errores} tipo(s) no se pudieron guardar`, 'Cerrar', { duration: 5000 });
      this.cargarTipos();
    } else {
      this.snackBar.open(`${total} tipo(s) actualizado(s)`, 'OK', { duration: 2000 });
    }
    this.cdr.markForCheck();
  }

  descartarCambiosTipos(): void {
    this.tipos = this.clonarTipos(this.tiposOriginales);
    this.cdr.markForCheck();
  }

  // --- Reglas de evaluacion ---

  cargarReglas(): void {
    this.cargandoReglas = true;
    this.api.getReglas().subscribe({
      next: (reglas) => {
        this.reglas = reglas;
        this.reglasOriginales = this.clonarReglas(reglas);
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

  getOperadorTexto(operador: number): string {
    const textos: Record<number, string> = {
      0: 'mayor a',
      1: 'menor a',
      2: 'mayor o igual a',
      3: 'menor o igual a',
      4: 'igual a',
      5: 'diferente de'
    };
    return textos[operador] ?? '?';
  }

  getResultadoNombre(resultado: number): string {
    return this.resultados.find(r => r.valor === resultado)?.nombre ?? '?';
  }

  getResultadoColor(resultado: number): string {
    return this.resultados.find(r => r.valor === resultado)?.color ?? '';
  }

  generarDescripcion(regla: ReglaEvaluacionConfig): string {
    const campo = regla.campo;
    const opTexto = this.getOperadorTexto(regla.operador);
    return `${campo} ${opTexto} ${regla.valor}`;
  }

  isReglaModificada(regla: ReglaEvaluacionConfig): boolean {
    return this.getReglasModificadasIds().has(regla.id);
  }

  // Cambios locales (sin guardar al backend)
  onReglaValorChange(regla: ReglaEvaluacionConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoValor = parseFloat(input.value);
    if (!isNaN(nuevoValor)) {
      regla.valor = nuevoValor;
      regla.descripcion = this.generarDescripcion(regla);
      this.cdr.markForCheck();
    }
  }

  onReglaPesoChange(regla: ReglaEvaluacionConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoPeso = parseFloat(input.value);
    if (!isNaN(nuevoPeso)) {
      regla.peso = nuevoPeso;
      this.cdr.markForCheck();
    }
  }

  onReglaOrdenChange(regla: ReglaEvaluacionConfig, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoOrden = parseInt(input.value, 10);
    if (!isNaN(nuevoOrden)) {
      regla.orden = nuevoOrden;
      this.cdr.markForCheck();
    }
  }

  onOperadorChange(regla: ReglaEvaluacionConfig, nuevoOperador: number): void {
    regla.operador = nuevoOperador;
    regla.descripcion = this.generarDescripcion(regla);
    this.cdr.markForCheck();
  }

  onResultadoChange(regla: ReglaEvaluacionConfig, nuevoResultado: number): void {
    regla.resultado = nuevoResultado;
    this.cdr.markForCheck();
  }

  toggleReglaActiva(regla: ReglaEvaluacionConfig, activa: boolean): void {
    regla.activa = activa;
    this.cdr.markForCheck();
  }

  // Guardar TODAS las reglas modificadas al backend
  guardarReglas(): void {
    const modificadas = this.getReglasModificadasIds();
    if (modificadas.size === 0) return;

    this.guardandoReglas = true;
    this.cdr.markForCheck();

    const reglasAGuardar = this.reglas.filter(r => modificadas.has(r.id));
    let completadas = 0;
    let errores = 0;

    for (const regla of reglasAGuardar) {
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
          completadas++;
          this.verificarGuardadoCompleto(completadas, errores, reglasAGuardar.length);
        },
        error: () => {
          errores++;
          completadas++;
          this.verificarGuardadoCompleto(completadas, errores, reglasAGuardar.length);
        }
      });
    }
  }

  private verificarGuardadoCompleto(completadas: number, errores: number, total: number): void {
    if (completadas < total) return;

    this.guardandoReglas = false;
    this.reglasOriginales = this.clonarReglas(this.reglas);

    if (errores > 0) {
      this.snackBar.open(`${errores} regla(s) no se pudieron guardar`, 'Cerrar', { duration: 5000 });
      this.cargarReglas();
    } else {
      this.snackBar.open(`${total} regla(s) guardada(s)`, 'OK', { duration: 2000 });
    }
    this.cdr.markForCheck();
  }

  descartarCambiosReglas(): void {
    this.reglas = this.clonarReglas(this.reglasOriginales);
    this.cdr.markForCheck();
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
            this.reglasOriginales = this.clonarReglas(this.reglas);
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
        this.reglasOriginales = this.reglasOriginales.filter(r => r.id !== regla.id);
        this.snackBar.open(`Regla "${regla.nombre}" eliminada`, 'OK', { duration: 3000 });
        this.cdr.markForCheck();
      },
      error: () => {
        this.snackBar.open(`Error al eliminar ${regla.nombre}`, 'Cerrar', { duration: 5000 });
      }
    });
  }

  // --- Parametros de linea de credito: dirty tracking ---

  get hayParametrosLCModificados(): boolean {
    const campos: (keyof ParametrosLineaCredito)[] = [
      'porcentajeCapitalTrabajo', 'porcentajePatrimonio', 'porcentajeUtilidadNeta',
      'pesoRedNivel0', 'pesoRedNivel1', 'pesoRedNivel2'
    ];
    return campos.some(c => this.parametrosLC[c] !== this.parametrosLCOriginal[c]);
  }

  cargarParametrosLC(): void {
    this.cargandoLC = true;
    this.api.getParametrosLineaCredito().subscribe({
      next: (params) => {
        this.parametrosLC = params;
        this.parametrosLCOriginal = { ...params };
        this.cargandoLC = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.snackBar.open('Error al cargar parametros de linea de credito', 'Cerrar', { duration: 5000 });
        this.cargandoLC = false;
        this.cdr.markForCheck();
      }
    });
  }

  onParametroLCChange(campo: keyof ParametrosLineaCredito, event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevoValor = parseFloat(input.value);
    if (!isNaN(nuevoValor)) {
      this.parametrosLC = { ...this.parametrosLC, [campo]: nuevoValor };
      this.cdr.markForCheck();
    }
  }

  guardarParametrosLC(): void {
    this.guardandoLC = true;
    this.cdr.markForCheck();

    this.api.actualizarParametrosLineaCredito(this.parametrosLC).subscribe({
      next: (params) => {
        this.parametrosLC = params;
        this.parametrosLCOriginal = { ...params };
        this.guardandoLC = false;
        this.snackBar.open('Parametros de linea de credito actualizados', 'OK', { duration: 2000 });
        this.cdr.markForCheck();
      },
      error: () => {
        this.guardandoLC = false;
        this.snackBar.open('Error al actualizar parametros', 'Cerrar', { duration: 5000 });
        this.cargarParametrosLC();
      }
    });
  }

  descartarCambiosLC(): void {
    this.parametrosLC = { ...this.parametrosLCOriginal };
    this.cdr.markForCheck();
  }
}
