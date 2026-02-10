import { Component } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-nuevo-expediente-dialog',
  standalone: true,
  imports: [MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, FormsModule],
  template: `
    <h2 mat-dialog-title>Nuevo Expediente</h2>
    <mat-dialog-content style="overflow: visible; padding-top: 8px;">
      <mat-form-field appearance="outline" style="width: 100%">
        <mat-label>Descripcion</mat-label>
        <input matInput [(ngModel)]="descripcion" maxlength="40"
               placeholder="Ej: Empresa XYZ - Credito comercial"
               (keydown.enter)="crear()">
        <mat-hint align="end">{{ descripcion.length }}/40</mat-hint>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-raised-button color="primary"
              [disabled]="!descripcion.trim()"
              (click)="crear()">
        Crear
      </button>
    </mat-dialog-actions>
  `
})
export class NuevoExpedienteDialogComponent {
  descripcion = '';

  constructor(private dialogRef: MatDialogRef<NuevoExpedienteDialogComponent>) {}

  crear(): void {
    if (this.descripcion.trim()) {
      this.dialogRef.close(this.descripcion.trim());
    }
  }
}
