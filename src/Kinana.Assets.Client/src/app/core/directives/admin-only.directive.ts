import { Directive, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Structural directive (R6.5): `*appAdminOnly` renders its host element only for
 * Admin users and removes it entirely otherwise.
 *
 * > **This is strictly for UX purposes to prevent visual clutter. Actual
 * > authorization is enforced server-side via the API.**
 */
@Directive({
  selector: '[appAdminOnly]',
  standalone: true
})
export class AdminOnlyDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authService = inject(AuthService);

  constructor() {
    effect(() => {
      const isAdmin = this.authService.user()?.role === 'Admin';
      this.viewContainer.clear();
      if (isAdmin) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      }
    });
  }
}
