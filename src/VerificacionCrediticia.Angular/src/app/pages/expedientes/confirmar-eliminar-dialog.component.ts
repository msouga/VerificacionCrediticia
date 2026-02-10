import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-confirmar-eliminar-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Confirmar eliminacion</h2>
    <mat-dialog-content>
      <p>Se eliminaran {{ data.cantidad }} expediente(s) y todos sus documentos asociados.</p>
      <p>Esta accion no se puede deshacer.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-raised-button color="warn" (click)="confirmar()">
        Eliminar
      </button>
    </mat-dialog-actions>
  `
})
export class ConfirmarEliminarDialogComponent {
  private dialogRef = inject<MatDialogRef<ConfirmarEliminarDialogComponent>>(MatDialogRef);
  data = inject<{
    cantidad: number;
}>(MAT_DIALOG_DATA);


  confirmar(): void {
    this.dialogRef.close(true);
  }
}
