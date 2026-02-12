import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, ViewChild, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SelectionModel } from '@angular/cdk/collections';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VerificacionApiService } from '../../services/verificacion-api.service';
import {
  ExpedienteResumen, EstadoExpediente, ListaExpedientesResponse
} from '../../models/expediente.model';
import { NuevoExpedienteDialogComponent } from './nuevo-expediente-dialog.component';
import { EditarDescripcionDialogComponent } from './editar-descripcion-dialog.component';
import { ConfirmarEliminarDialogComponent } from './confirmar-eliminar-dialog.component';

@Component({
  selector: 'app-expedientes',
  standalone: true,
  imports: [
    MatTableModule, MatPaginatorModule, MatCheckboxModule, MatButtonModule,
    MatIconModule, MatCardModule, MatChipsModule, MatProgressSpinnerModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatTooltipModule,
    DatePipe, FormsModule
  ],
  templateUrl: './expedientes.component.html',
  styleUrl: './expedientes.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExpedientesComponent implements OnInit {
  private api = inject(VerificacionApiService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private cdr = inject(ChangeDetectorRef);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedColumns = ['select', 'id', 'descripcion', 'dniSolicitante', 'rucEmpresa', 'razonSocialEmpresa', 'estado', 'fechaCreacion', 'acciones'];
  dataSource: ExpedienteResumen[] = [];
  selection = new SelectionModel<ExpedienteResumen>(true, []);

  totalItems = 0;
  pagina = 1;
  tamanoPagina = 10;
  loading = false;
  eliminando = false;

  ngOnInit(): void {
    this.cargarExpedientes();
  }

  cargarExpedientes(): void {
    this.loading = true;
    this.selection.clear();
    this.cdr.markForCheck();

    this.api.listarExpedientes(this.pagina, this.tamanoPagina).subscribe({
      next: (res: ListaExpedientesResponse) => {
        this.dataSource = res.items;
        this.totalItems = res.total;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pagina = event.pageIndex + 1;
    this.tamanoPagina = event.pageSize;
    this.cargarExpedientes();
  }

  isAllSelected(): boolean {
    return this.selection.selected.length === this.dataSource.length && this.dataSource.length > 0;
  }

  toggleAllRows(): void {
    if (this.isAllSelected()) {
      this.selection.clear();
    } else {
      this.selection.select(...this.dataSource);
    }
  }

  irAExpediente(row: ExpedienteResumen): void {
    this.router.navigate(['/expediente', row.id]);
  }

  nuevoExpediente(): void {
    const dialogRef = this.dialog.open(NuevoExpedienteDialogComponent, {
      width: '400px'
    });

    dialogRef.afterClosed().subscribe((descripcion: string | undefined) => {
      if (descripcion) {
        this.api.crearExpediente({ descripcion }).subscribe({
          next: (exp) => {
            this.router.navigate(['/expediente', exp.id]);
          }
        });
      }
    });
  }

  editarDescripcion(event: Event, row: ExpedienteResumen): void {
    event.stopPropagation();
    const dialogRef = this.dialog.open(EditarDescripcionDialogComponent, {
      width: '400px',
      data: { descripcion: row.descripcion }
    });

    dialogRef.afterClosed().subscribe((descripcion: string | undefined) => {
      if (descripcion && descripcion !== row.descripcion) {
        this.api.actualizarExpediente(row.id, { descripcion }).subscribe({
          next: () => this.cargarExpedientes()
        });
      }
    });
  }

  eliminarSeleccionados(): void {
    const ids = this.selection.selected.map(e => e.id);
    if (ids.length === 0) return;

    const dialogRef = this.dialog.open(ConfirmarEliminarDialogComponent, {
      width: '400px',
      data: { cantidad: ids.length }
    });

    dialogRef.afterClosed().subscribe((confirmado: boolean) => {
      if (confirmado) {
        this.eliminando = true;
        this.cdr.markForCheck();

        this.api.eliminarExpedientes(ids).subscribe({
          next: () => {
            this.eliminando = false;
            this.cargarExpedientes();
          },
          error: () => {
            this.eliminando = false;
            this.cdr.markForCheck();
          }
        });
      }
    });
  }

  getEstadoLabel(row: ExpedienteResumen): string {
    if (row.estado === EstadoExpediente.Evaluado && row.recomendacion !== null) {
      switch (row.recomendacion) {
        case 0: return 'Aprobado';
        case 1: return 'Revision Manual';
        case 2: return 'Rechazado';
      }
    }
    const labels: Record<number, string> = {
      [EstadoExpediente.Iniciado]: 'Iniciado',
      [EstadoExpediente.EnProceso]: 'En Proceso',
      [EstadoExpediente.DocumentosCompletos]: 'Docs. Completos',
      [EstadoExpediente.Evaluado]: 'Evaluado'
    };
    return labels[row.estado] || 'Desconocido';
  }

  getEstadoColor(row: ExpedienteResumen): string {
    if (row.estado === EstadoExpediente.Evaluado && row.recomendacion !== null) {
      switch (row.recomendacion) {
        case 0: return 'success';
        case 1: return 'warning';
        case 2: return 'error';
      }
    }
    const colors: Record<number, string> = {
      [EstadoExpediente.Iniciado]: 'default',
      [EstadoExpediente.EnProceso]: 'accent',
      [EstadoExpediente.DocumentosCompletos]: 'primary',
      [EstadoExpediente.Evaluado]: 'primary'
    };
    return colors[row.estado] || 'default';
  }
}
