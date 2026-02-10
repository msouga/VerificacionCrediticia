import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatSidenavModule, MatToolbarModule, MatListModule,
    MatIconModule, MatButtonModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent {
  sidenavOpened = true;

  navItems = [
    { label: 'Dashboard', icon: 'dashboard', route: '/' },
    { label: 'Expedientes', icon: 'folder_open', route: '/expedientes' },
    { label: 'Evaluar', icon: 'search', route: '/evaluar' },
    { label: 'Historial', icon: 'history', route: '/historial' },
    { label: 'Configuracion', icon: 'settings', route: '/configuracion' }
  ];
}
