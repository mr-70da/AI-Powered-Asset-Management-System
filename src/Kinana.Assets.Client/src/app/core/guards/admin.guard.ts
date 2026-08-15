import { inject } from '@angular/core';
import { CanMatchFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Admin-only guard used with `canMatch` on lazy-loaded routes (R6.2).
 * Because it runs before the lazy chunk is requested, a standard User never
 * even downloads the Create/Edit/Transfer feature bundles.
 */
export const adminGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  return authService.isAdmin();
};
