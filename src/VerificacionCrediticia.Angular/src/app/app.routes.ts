import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { HomeComponent } from './pages/home/home.component';
import { EvaluarComponent } from './pages/evaluar/evaluar.component';
import { HistorialComponent } from './pages/historial/historial.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', component: HomeComponent },
      { path: 'evaluar', component: EvaluarComponent },
      { path: 'historial', component: HistorialComponent }
    ]
  }
];
