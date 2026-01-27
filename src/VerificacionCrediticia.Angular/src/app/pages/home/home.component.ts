import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DatePipe, DecimalPipe } from '@angular/common';

interface EvaluacionReciente {
  nombre: string;
  dni: string;
  empresa: string;
  ruc: string;
  score: number;
  resultado: string;
}

interface AlertaReciente {
  mensaje: string;
  severidad: string;
  tiempo: string;
}

interface TendenciaDia {
  dia: string;
  evaluaciones: number;
  porcentaje: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    RouterLink, MatCardModule, MatButtonModule, MatIconModule,
    MatTableModule, MatChipsModule, MatProgressBarModule,
    DatePipe, DecimalPipe
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  today = new Date();

  stats = {
    evaluacionesHoy: 47,
    aprobadas: 28,
    enRevision: 12,
    rechazadas: 7,
    porcentajeAprobacion: 60,
    porcentajeRevision: 25,
    porcentajeRechazo: 15,
    evaluacionesMes: 892,
    scorePromedio: 72.4,
    empresasUnicas: 234
  };

  tendencia: TendenciaDia[] = [
    { dia: 'Lunes', evaluaciones: 42, porcentaje: 70 },
    { dia: 'Martes', evaluaciones: 38, porcentaje: 63 },
    { dia: 'Miercoles', evaluaciones: 55, porcentaje: 92 },
    { dia: 'Jueves', evaluaciones: 60, porcentaje: 100 },
    { dia: 'Viernes', evaluaciones: 51, porcentaje: 85 },
    { dia: 'Sabado', evaluaciones: 12, porcentaje: 20 },
    { dia: 'Hoy', evaluaciones: 47, porcentaje: 78 }
  ];

  evaluacionesRecientes: EvaluacionReciente[] = [
    { nombre: 'Carlos Mendoza', dni: '45678912', empresa: 'Tech Solutions SAC', ruc: '20512345678', score: 85.5, resultado: 'APROBADO' },
    { nombre: 'Ana Torres', dni: '78912345', empresa: 'Comercial Norte EIRL', ruc: '20498765432', score: 45.2, resultado: 'RECHAZADO' },
    { nombre: 'Jose Garcia', dni: '12345678', empresa: 'Distribuidora Sur SAC', ruc: '20567891234', score: 68.0, resultado: 'REVISION' },
    { nombre: 'Maria Lopez', dni: '89123456', empresa: 'Importaciones Lima SAC', ruc: '20612345789', score: 92.3, resultado: 'APROBADO' },
    { nombre: 'Pedro Sanchez', dni: '56789123', empresa: 'Servicios Globales EIRL', ruc: '20789456123', score: 71.8, resultado: 'REVISION' }
  ];
  displayedColumns = ['nombre', 'empresa', 'score', 'resultado'];

  alertasRecientes: AlertaReciente[] = [
    { mensaje: 'Empresa con deuda castigada detectada: Comercial Norte EIRL', severidad: 'error', tiempo: '42 min' },
    { mensaje: 'Score bajo en solicitante relacionado: Pedro Quispe', severidad: 'warning', tiempo: '1 hora' },
    { mensaje: 'Morosidad detectada en red de relaciones', severidad: 'warning', tiempo: '2 horas' },
    { mensaje: 'Empresa inactiva en SUNAT: Servicios XYZ SAC', severidad: 'error', tiempo: '3 horas' }
  ];

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
}
