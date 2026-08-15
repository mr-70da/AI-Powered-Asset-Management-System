import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, from, switchMap, throwError, type Observable } from 'rxjs';
import { TokenStorageService } from '../services/token-storage.service';
import { AuthService } from '../services/auth.service';

const AUTH_ENDPOINTS = ['/api/auth/login', '/api/auth/refresh'];
const RETRY_MARKER = 'X-Kinana-Retry-Attempted';

/**
 * Central HTTP concern handling (R6.3):
 *  - attaches `Authorization: Bearer <token>` to every outgoing request;
 *  - on 401, tries a single token refresh and retries the original request,
 *    falling back to a sign-out + redirect to /login;
 *  - on 403, redirects to the "Not Permitted" route.
 *
 * Components never see token plumbing or response-status juggling.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);
  const authService = inject(AuthService);

  const isAuthCall = AUTH_ENDPOINTS.some((url) => req.url.includes(url));
  const token = tokenStorage.accessToken;

  const request = isAuthCall
    ? req
    : req.clone({ setHeaders: { Authorization: `Bearer ${token ?? ''}` } });

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 403) {
        router.navigate(['/not-permitted']);
        return throwError(() => error);
      }

      if (error.status === 401 && !isAuthCall && tokenStorage.refreshToken && !req.headers.has(RETRY_MARKER)) {
        return singleFlightRefresh(authService, tokenStorage).pipe(
          switchMap(() =>
            next(
              req.clone({
                setHeaders: { Authorization: `Bearer ${tokenStorage.accessToken ?? ''}`, [RETRY_MARKER]: 'true' }
              })
            )
          ),
          catchError((refreshError) => {
            authService.logout();
            router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
            return throwError(() => refreshError);
          })
        );
      }

      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }

      return throwError(() => error);
    })
  );
};

let refreshPromise: Promise<void> | null = null;

/** Ensures concurrent 401s trigger a single refresh request, not a stampede. */
function singleFlightRefresh(authService: AuthService, tokenStorage: TokenStorageService): Observable<void> {
  if (!refreshPromise) {
    refreshPromise = firstValueFrom(authService.refresh())
      .then((auth) => {
        tokenStorage.storeTokens(auth.accessToken, auth.refreshToken, auth.expiresAtUtc);
        if (authService.user()) {
          authService.setCurrentUser({ ...authService.user()!, role: auth.role });
        }
      })
      .finally(() => {
        refreshPromise = null;
      });
  }
  return from(refreshPromise);
}
