import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { HomeComponent } from './pages/home/home.component';
import { EvaluarComponent } from './pages/evaluar/evaluar.component';
import { HistorialComponent } from './pages/historial/historial.component';
import { ExpedientesComponent } from './pages/expedientes/expedientes.component';
import { ExpedienteComponent } from './pages/expediente/expediente.component';
import { ConfiguracionComponent } from './pages/configuracion/configuracion.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', component: HomeComponent },
      { path: 'expedientes', component: ExpedientesComponent },
      { path: 'expediente/:id', component: ExpedienteComponent },
      { path: 'evaluar', component: EvaluarComponent },
      { path: 'historial', component: HistorialComponent },
      { path: 'configuracion', component: ConfiguracionComponent }
    ]
  }
];
