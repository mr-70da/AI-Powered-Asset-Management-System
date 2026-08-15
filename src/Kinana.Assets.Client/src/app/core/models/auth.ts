export type UserRole = 'Admin' | 'User';

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  role: UserRole;
}

export interface UserProfile {
  id: number;
  userName: string;
  displayName: string;
  email: string;
  role: UserRole;
  isDisabled: boolean;
  createdAtUtc: string;
}
