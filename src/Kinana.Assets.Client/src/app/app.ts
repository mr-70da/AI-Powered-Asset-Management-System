import { Component, inject, signal, type Signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { AdminOnlyDirective } from './core/directives/admin-only.directive';
import type { UserProfile } from './core/models/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AdminOnlyDirective],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly user: Signal<UserProfile | null> = this.authService.user;

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
