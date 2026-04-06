import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from './data/service/auth.service';
import { map } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatTabsModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class AppComponent {
  private authService = inject(AuthService);
  isAuthenticated$ = this.authService.checkAuthStatus().pipe(
    map(status => status.isAuthenticated)
  );

  logout(): void {
    this.authService.logout().subscribe({
      next: () => console.log('Sesión cerrada'),
      error: (err) => console.error('Error al cerrar sesión', err)
    });
  }
}