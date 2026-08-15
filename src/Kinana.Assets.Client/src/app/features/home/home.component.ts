import { Component, inject, signal, type Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AdminOnlyDirective } from '../../core/directives/admin-only.directive';
import type { UserProfile } from '../../core/models/auth';

@Component({
  selector: 'app-home',
  imports: [RouterLink, AdminOnlyDirective],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  private readonly authService = inject(AuthService);

  readonly user: Signal<UserProfile | null> = this.authService.user;
}
