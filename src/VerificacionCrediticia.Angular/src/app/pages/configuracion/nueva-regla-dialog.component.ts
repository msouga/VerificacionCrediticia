import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CrearReglaRequest } from '../../models/configuracion.model';

@Component({
  selector: 'app-nueva-regla-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Nueva Regla de Evaluacion</h2>
    <mat-dialog-content>
      <div class="form-grid">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput [(ngModel)]="regla.nombre" required>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Descripcion</mat-label>
          <textarea matInput [(ngModel)]="regla.descripcion" rows="2"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Campo</mat-label>
          <mat-select [(ngModel)]="regla.campo" required>
            @for (campo of campos; track campo) {
              <mat-option [value]="campo">{{ campo }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Operador</mat-label>
          <mat-select [(ngModel)]="regla.operador" required>
            @for (op of operadores; track op.valor) {
              <mat-option [value]="op.valor">{{ op.simbolo }} ({{ op.nombre }})</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Valor</mat-label>
          <input matInput type="number" [(ngModel)]="regla.valor" required>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Peso (0 a 1)</mat-label>
          <input matInput type="number" [(ngModel)]="regla.peso" min="0" max="1" step="0.05" required>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Resultado</mat-label>
          <mat-select [(ngModel)]="regla.resultado" required>
            @for (res of resultados; track res.valor) {
              <mat-option [value]="res.valor">{{ res.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Orden</mat-label>
          <input matInput type="number" [(ngModel)]="regla.orden" min="0" required>
        </mat-form-field>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="!esValida()" (click)="guardar()">
        Crear
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0 16px;
      min-width: 480px;
    }
    .form-grid mat-form-field:first-child,
    .form-grid mat-form-field:nth-child(2) {
      grid-column: 1 / -1;
    }
  `]
})
export class NuevaReglaDialogComponent {
  private dialogRef = inject<MatDialogRef<NuevaReglaDialogComponent>>(MatDialogRef);

  regla: CrearReglaRequest = {
    nombre: '',
    descripcion: null,
    campo: '',
    operador: 2,  // >=
    valor: 0,
    peso: 0.1,
    resultado: 0, // Aprobar
    orden: 0
  };

  campos = [
    'Liquidez', 'Endeudamiento', 'ScoreCrediticio', 'DeudaVencida',
    'MargenNeto', 'MargenBruto', 'MargenOperativo', 'Solvencia'
  ];

  operadores = [
    { valor: 0, simbolo: '>', nombre: 'Mayor que' },
    { valor: 1, simbolo: '<', nombre: 'Menor que' },
    { valor: 2, simbolo: '>=', nombre: 'Mayor o igual' },
    { valor: 3, simbolo: '<=', nombre: 'Menor o igual' },
    { valor: 4, simbolo: '==', nombre: 'Igual a' },
    { valor: 5, simbolo: '!=', nombre: 'Diferente de' }
  ];

  resultados = [
    { valor: 0, nombre: 'Aprobar' },
    { valor: 1, nombre: 'Rechazar' },
    { valor: 2, nombre: 'Revisar' }
  ];

  esValida(): boolean {
    return this.regla.nombre.trim().length > 0
      && this.regla.campo.length > 0
      && this.regla.peso >= 0 && this.regla.peso <= 1;
  }

  guardar(): void {
    if (this.esValida()) {
      this.dialogRef.close(this.regla);
    }
  }
}
