import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { NodoRed } from '../../models/resultado-evaluacion.model';
import { EstadoCrediticio, TipoNodo } from '../../models/enums';
import cytoscape from 'cytoscape';

@Component({
  selector: 'app-grafo-red',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatChipsModule],
  templateUrl: './grafo-red.component.html',
  styleUrls: ['./grafo-red.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GrafoRedComponent implements OnChanges, AfterViewInit {
  @Input() grafo?: { [key: string]: NodoRed };
  @Input() dniSolicitante = '';
  @Input() rucEmpresa = '';

  @ViewChild('cyContainer', { static: false }) cyContainer!: ElementRef;

  private cy?: cytoscape.Core;
  nodoSeleccionado?: NodoRed;
  viewReady = false;

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.renderGraph();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['grafo'] && this.viewReady) {
      this.renderGraph();
    }
  }

  private renderGraph(): void {
    if (!this.grafo || !this.cyContainer) return;

    const elements: cytoscape.ElementDefinition[] = [];
    const edgesAdded = new Set<string>();

    // Crear nodos
    for (const [id, nodo] of Object.entries(this.grafo)) {
      const esSolicitante = id === this.dniSolicitante;
      const esEmpresaPrincipal = id === this.rucEmpresa;

      const esEmpresa = this.esEmpresaTipo(nodo.tipo);

      elements.push({
        data: {
          id,
          label: this.truncarNombre(nodo.nombre),
          fullName: nodo.nombre,
          tipo: nodo.tipo,
          score: nodo.score,
          estado: nodo.estadoCredito,
          nivel: nodo.nivelProfundidad,
          esPrincipal: esSolicitante || esEmpresaPrincipal,
          color: this.getColorNodo(nodo.estadoCredito),
          borderColor: this.getBorderColor(esSolicitante, esEmpresaPrincipal),
          shape: esEmpresa ? 'round-rectangle' : 'ellipse',
          width: esEmpresa ? 60 : 50,
          height: esEmpresa ? 40 : 50,
          borderWidth: (esSolicitante || esEmpresaPrincipal) ? 4 : 2,
          fontSize: (esSolicitante || esEmpresaPrincipal) ? 11 : 9,
        }
      });

      // Crear aristas desde conexiones (solo si el nodo destino existe en el grafo)
      for (const conexion of nodo.conexiones) {
        if (!this.grafo![conexion.identificador]) continue;
        const edgeId = [id, conexion.identificador].sort().join('-');
        if (!edgesAdded.has(edgeId)) {
          edgesAdded.add(edgeId);
          elements.push({
            data: {
              id: `edge-${edgeId}`,
              source: id,
              target: conexion.identificador,
              label: conexion.tipoRelacion,
            }
          });
        }
      }
    }

    if (this.cy) {
      this.cy.destroy();
    }

    this.cy = cytoscape({
      container: this.cyContainer.nativeElement,
      elements,
      style: [
        {
          selector: 'node',
          style: {
            'label': 'data(label)',
            'text-valign': 'bottom',
            'text-halign': 'center',
            'text-margin-y': 6,
            'font-size': 'data(fontSize)',
            'background-color': 'data(color)',
            'border-color': 'data(borderColor)',
            'border-width': 'data(borderWidth)',
            'shape': 'data(shape)' as any,
            'width': 'data(width)',
            'height': 'data(height)',
            'text-wrap': 'wrap',
            'text-max-width': '80px',
            'color': '#333',
          }
        },
        {
          selector: 'edge',
          style: {
            'width': 1.5,
            'line-color': '#999',
            'target-arrow-color': '#999',
            'curve-style': 'bezier',
            'label': 'data(label)',
            'font-size': 8,
            'text-rotation': 'autorotate',
            'color': '#666',
            'text-background-color': '#fff',
            'text-background-opacity': 0.8,
            'text-background-padding': '2px' as any,
          }
        },
        {
          selector: 'node:selected',
          style: {
            'border-color': '#1976d2',
            'border-width': 4,
            'overlay-color': '#1976d2',
            'overlay-opacity': 0.1,
          }
        }
      ],
      layout: {
        name: 'cose',
        fit: true,
        animate: true,
        animationDuration: 500,
        nodeRepulsion: () => 8000,
        idealEdgeLength: () => 120,
        gravity: 0.5,
        padding: 50,
      } as any,
      minZoom: 0.3,
      maxZoom: 3,
      userPanningEnabled: true,
      userZoomingEnabled: true,
    });

    // Click en nodo para mostrar detalle
    this.cy.on('tap', 'node', (evt) => {
      const nodeId = evt.target.id();
      this.nodoSeleccionado = this.grafo?.[nodeId];
    });

    // Click en fondo para deseleccionar
    this.cy.on('tap', (evt) => {
      if (evt.target === this.cy) {
        this.nodoSeleccionado = undefined;
      }
    });
  }

  private truncarNombre(nombre: string): string {
    if (nombre.length <= 20) return nombre;
    return nombre.substring(0, 18) + '...';
  }

  // El backend puede enviar enums como numeros (0,1,2...) o como strings
  private esEmpresaTipo(tipo: TipoNodo | number | string): boolean {
    return tipo === TipoNodo.Empresa || tipo === 1 || tipo === 'Empresa';
  }

  private getColorNodo(estado: EstadoCrediticio | number | string): string {
    const s = typeof estado === 'number' ? estado : estado?.toString();
    if (s === EstadoCrediticio.Normal || s === 0) return '#4caf50';
    if (s === EstadoCrediticio.ConProblemasPotenciales || s === 1) return '#ff9800';
    if (s === EstadoCrediticio.Moroso || s === 2) return '#f44336';
    if (s === EstadoCrediticio.EnCobranza || s === 3) return '#d32f2f';
    if (s === EstadoCrediticio.Castigado || s === 4) return '#b71c1c';
    if (s === EstadoCrediticio.SinInformacion || s === 5) return '#9e9e9e';
    return '#9e9e9e';
  }

  private getBorderColor(esSolicitante: boolean, esEmpresa: boolean): string {
    if (esSolicitante) return '#1565c0';
    if (esEmpresa) return '#4527a0';
    return '#757575';
  }

  private static readonly estadoLabels: Record<number | string, string> = {
    0: 'Normal', 'Normal': 'Normal',
    1: 'Problemas Potenciales', 'ConProblemasPotenciales': 'Problemas Potenciales',
    2: 'Moroso', 'Moroso': 'Moroso',
    3: 'En Cobranza', 'EnCobranza': 'En Cobranza',
    4: 'Castigado', 'Castigado': 'Castigado',
    5: 'Sin Informacion', 'SinInformacion': 'Sin Informacion',
  };

  private static readonly tipoLabels: Record<number | string, string> = {
    0: 'Persona', 'Persona': 'Persona',
    1: 'Empresa', 'Empresa': 'Empresa',
  };

  getEstadoLabel(estado: EstadoCrediticio | number | string): string {
    return GrafoRedComponent.estadoLabels[estado] ?? String(estado);
  }

  getTipoLabel(tipo: TipoNodo | number | string): string {
    return GrafoRedComponent.tipoLabels[tipo] ?? String(tipo);
  }

  getEstadoColor(estado: EstadoCrediticio | number | string): string {
    const s = typeof estado === 'number' ? estado : estado?.toString();
    if (s === EstadoCrediticio.Normal || s === 0) return 'primary';
    if (s === EstadoCrediticio.ConProblemasPotenciales || s === 1) return 'accent';
    if (s === EstadoCrediticio.Moroso || s === 2 ||
        s === EstadoCrediticio.EnCobranza || s === 3 ||
        s === EstadoCrediticio.Castigado || s === 4) return 'warn';
    return '';
  }

  cerrarDetalle(): void {
    this.nodoSeleccionado = undefined;
    this.cy?.elements().unselect();
  }
}
