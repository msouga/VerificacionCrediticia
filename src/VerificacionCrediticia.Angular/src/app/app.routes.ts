import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent) },
      { path: 'expedientes', loadComponent: () => import('./pages/expedientes/expedientes.component').then(m => m.ExpedientesComponent) },
      { path: 'expediente/:id', loadComponent: () => import('./pages/expediente/expediente.component').then(m => m.ExpedienteComponent) },
      {
        path: 'configuracion',
        loadComponent: () => import('./pages/configuracion/configuracion.component').then(m => m.ConfiguracionComponent),
        canDeactivate: [(component: { canDeactivate: () => boolean }) => component.canDeactivate()]
      }
    ]
  }
];
