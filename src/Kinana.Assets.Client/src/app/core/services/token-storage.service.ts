import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'kinana.access_token';
const REFRESH_TOKEN_KEY = 'kinana.refresh_token';
const EXPIRES_AT_KEY = 'kinana.expires_at_utc';

/**
 * Persists the JWT pair in localStorage.
 *
 * Rationale: localStorage survives tab closes and full page reloads, so a
 * user only signs in once per browser session instead of every refresh.
 * The trade-off is exposure to XSS — any script injected into the page could
 * read the tokens. That risk is mitigated (never eliminated) by Angular's
 * built-in template escaping, avoiding innerHTML / bypassSecurityTrustHtml,
 * and by treating the API as the real authorization boundary. An alternative
 * (in-memory storage) is safer against XSS but loses the session on refresh
 * and would force a re-login or silent refresh dance on every page load.
 *
 * See the README section "Token storage rationale (R6.8)" for the full write-up.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  get expiresAtUtc(): string | null {
    return localStorage.getItem(EXPIRES_AT_KEY);
  }

  storeTokens(accessToken: string, refreshToken: string, expiresAtUtc: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    localStorage.setItem(EXPIRES_AT_KEY, expiresAtUtc);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
  }

  isExpired(now = new Date()): boolean {
    const expiresAt = this.expiresAtUtc;
    return !expiresAt || isNaN(Date.parse(expiresAt)) || new Date(expiresAt) <= now;
  }
}
