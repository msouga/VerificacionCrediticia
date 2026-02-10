import { Component, OnInit, ViewChild, DestroyRef, inject, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { DecimalPipe, DatePipe } from '@angular/common';
import { debounceTime } from 'rxjs';

interface EvaluacionHistorial {
  id: string;
  fecha: Date;
  nombreSolicitante: string;
  dniSolicitante: string;
  razonSocialEmpresa: string;
  rucEmpresa: string;
  scoreFinal: number;
  scoreSolicitante: number;
  scoreEmpresa: number;
  scoreRed: number;
  resultado: string;
  alertas: number;
  alertaCritica: boolean;
}

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatDatepickerModule, MatNativeDateModule,
    MatButtonModule, MatIconModule, MatTableModule, MatPaginatorModule,
    MatSortModule, MatChipsModule, MatProgressBarModule, MatBadgeModule,
    MatDialogModule, DecimalPipe, DatePipe
  ],
  templateUrl: './historial.component.html',
  styleUrl: './historial.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistorialComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  searchControl = new FormControl('');
  resultadoFilter = new FormControl('');
  fechaDesde = new FormControl<Date | null>(null);
  fechaHasta = new FormControl<Date | null>(null);

  dataSource = new MatTableDataSource<EvaluacionHistorial>();
  displayedColumns = ['fecha', 'id', 'solicitante', 'empresa', 'score', 'desglose', 'resultado', 'alertas', 'acciones'];

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  allData: EvaluacionHistorial[] = [];
  selectedEval: EvaluacionHistorial | null = null;

  private nombres = [
    'Carlos Mendoza', 'Ana Torres', 'Jose Garcia', 'Maria Lopez', 'Pedro Sanchez',
    'Luis Fernandez', 'Rosa Martinez', 'Juan Perez', 'Carmen Rodriguez', 'Miguel Chavez',
    'Sofia Diaz', 'Diego Vargas', 'Lucia Flores', 'Andres Quispe', 'Patricia Huaman'
  ];

  private empresas = [
    'Tech Solutions SAC', 'Comercial Norte EIRL', 'Distribuidora Sur SAC',
    'Importaciones Lima SAC', 'Servicios Globales EIRL', 'Constructora Peruana SAC',
    'Alimentos del Peru EIRL', 'Textiles Modernos SAC', 'Transportes Rapidos SAC',
    'Consultores Unidos EIRL'
  ];

  constructor(private dialog: MatDialog) {}

  ngOnInit(): void {
    this.generateMockData();

    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      this.applyFilters();
    });
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  private seededRandom(seed: number): () => number {
    let s = seed;
    return () => {
      s = (s * 16807) % 2147483647;
      return (s - 1) / 2147483646;
    };
  }

  private generateMockData(): void {
    const rand = this.seededRandom(42);
    const data: EvaluacionHistorial[] = [];

    for (let i = 0; i < 150; i++) {
      const score = Math.round((30 + rand() * 60) * 10) / 10;
      const scoreSol = Math.round((50 + rand() * 50) * 10) / 10;
      const scoreEmp = Math.round((20 + rand() * 70) * 10) / 10;
      const scoreRed = Math.round((40 + rand() * 60) * 10) / 10;
      const resultado = score >= 70 ? 'APROBADO' : score >= 50 ? 'REVISION' : 'RECHAZADO';
      const alertCount = resultado === 'RECHAZADO' ? Math.floor(2 + rand() * 5) :
                         resultado === 'REVISION' ? Math.floor(1 + rand() * 3) :
                         Math.floor(rand() * 3);

      const daysAgo = Math.floor(rand() * 30);
      const fecha = new Date();
      fecha.setDate(fecha.getDate() - daysAgo);
      fecha.setHours(Math.floor(8 + rand() * 10), Math.floor(rand() * 60));

      const nombreIdx = Math.floor(rand() * this.nombres.length);
      const empresaIdx = Math.floor(rand() * this.empresas.length);

      data.push({
        id: `VER-${String(2024000 + i).padStart(7, '0')}`,
        fecha,
        nombreSolicitante: this.nombres[nombreIdx],
        dniSolicitante: String(10000000 + Math.floor(rand() * 89999999)),
        razonSocialEmpresa: this.empresas[empresaIdx],
        rucEmpresa: String(20100000000 + Math.floor(rand() * 899999999)),
        scoreFinal: score,
        scoreSolicitante: scoreSol,
        scoreEmpresa: scoreEmp,
        scoreRed: scoreRed,
        resultado,
        alertas: alertCount,
        alertaCritica: alertCount > 2 && resultado === 'RECHAZADO'
      });
    }

    data.sort((a, b) => b.fecha.getTime() - a.fecha.getTime());
    this.allData = data;
    this.dataSource.data = data;
  }

  applyFilters(): void {
    let filtered = [...this.allData];
    const search = (this.searchControl.value || '').toLowerCase();
    const resultado = this.resultadoFilter.value;
    const desde = this.fechaDesde.value;
    const hasta = this.fechaHasta.value;

    if (search) {
      filtered = filtered.filter(e =>
        e.nombreSolicitante.toLowerCase().includes(search) ||
        e.dniSolicitante.includes(search) ||
        e.razonSocialEmpresa.toLowerCase().includes(search) ||
        e.rucEmpresa.includes(search)
      );
    }

    if (resultado) {
      filtered = filtered.filter(e => e.resultado === resultado);
    }

    if (desde) {
      filtered = filtered.filter(e => e.fecha >= desde);
    }

    if (hasta) {
      const end = new Date(hasta);
      end.setHours(23, 59, 59);
      filtered = filtered.filter(e => e.fecha <= end);
    }

    this.dataSource.data = filtered;
    if (this.paginator) this.paginator.firstPage();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.resultadoFilter.setValue('');
    this.fechaDesde.setValue(null);
    this.fechaHasta.setValue(null);
    this.dataSource.data = this.allData;
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'success';
    if (score >= 60) return 'warning';
    return 'error';
  }

  getResultColor(resultado: string): string {
    switch (resultado) {
      case 'APROBADO': return 'success';
      case 'REVISION': return 'warning';
      case 'RECHAZADO': return 'error';
      default: return '';
    }
  }

  get totalFiltered(): number { return this.dataSource.data.length; }
  get aprobadas(): number { return this.dataSource.data.filter(e => e.resultado === 'APROBADO').length; }
  get enRevision(): number { return this.dataSource.data.filter(e => e.resultado === 'REVISION').length; }
  get rechazadas(): number { return this.dataSource.data.filter(e => e.resultado === 'RECHAZADO').length; }

  viewDetail(ev: EvaluacionHistorial): void {
    this.selectedEval = ev;
  }

  closeDetail(): void {
    this.selectedEval = null;
  }
}
