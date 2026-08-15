import { Injectable, signal, type WritableSignal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from './token-storage.service';
import type { AuthResponse, LoginRequest, RefreshRequest, UserProfile } from '../models/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal: WritableSignal<UserProfile | null> = signal(null);

  /** The signed-in user (null when signed out). Reacts to login/logout/session restore. */
  readonly user = this.userSignal.asReadonly();

  constructor(
    private readonly http: HttpClient,
    private readonly tokenStorage: TokenStorageService
  ) {}

  login(userName: string, password: string): Observable<AuthResponse> {
    const body: LoginRequest = { userName, password };
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, body);
  }

  /** Signs in, persists the token pair and fetches the caller's profile. */
  loginAndLoadProfile(userName: string, password: string): Observable<UserProfile> {
    return this.login(userName, password).pipe(
      switchMap((auth) => {
        this.setSession(auth);
        return this.getProfile().pipe(
          map((profile) => {
            this.setCurrentUser(profile);
            return profile;
          })
        );
      })
    );
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.tokenStorage.refreshToken;
    if (!refreshToken) {
      throw new Error('No refresh token available.');
    }
    const body: RefreshRequest = { refreshToken };
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/refresh`, body);
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${environment.apiUrl}/api/auth/me`);
  }

  /** Restores the current user from the token stored on a previous visit. */
  restoreSession(): Observable<unknown> {
    if (!this.tokenStorage.accessToken) {
      this.userSignal.set(null);
      return of(null);
    }
    // A stored-but-expired token is fine here: the request will 401, the
    // interceptor will refresh the token pair, and the call is retried.
    return this.getProfile().pipe(
      map((profile) => this.userSignal.set(profile)),
      catchError(() => {
        this.userSignal.set(null);
        return of(null);
      })
    );
  }

  setSession(auth: AuthResponse): void {
    this.tokenStorage.storeTokens(auth.accessToken, auth.refreshToken, auth.expiresAtUtc);
    this.userSignal.set({
      id: 0,
      userName: '',
      displayName: '',
      email: '',
      role: auth.role,
      isDisabled: false,
      createdAtUtc: ''
    });
  }

  setCurrentUser(profile: UserProfile): void {
    this.userSignal.set(profile);
  }

  logout(): void {
    this.tokenStorage.clear();
    this.userSignal.set(null);
  }

  isAuthenticated(): boolean {
    // Presence-based: an expired token still counts so the guard lets the user
    // through and the interceptor silently refreshes on the first 401. Only a
    // completely missing token (or a failed refresh) ends up at /login.
    return !!this.tokenStorage.accessToken;
  }

  isAdmin(): boolean {
    return this.userSignal()?.role === 'Admin';
  }
}
