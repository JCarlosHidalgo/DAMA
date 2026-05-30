import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { JwtClaims, UserRole } from './jwt.model';
import { SessionStorageTokenStorage } from './token-storage';
import { TokenDecoder } from './token-decoder';

interface LoginRequest {
  username: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
}

const DEFAULT_TENANT_TIMEZONE = 'America/La_Paz';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly httpClient = inject(HttpClient);
  private readonly tokenStorage = inject(SessionStorageTokenStorage);
  private readonly tokenDecoder = inject(TokenDecoder);

  private readonly tokenSignal = signal<string | null>(this.tokenStorage.read());

  readonly claims = computed<JwtClaims | null>(() => {
    const token = this.tokenSignal();
    return token ? this.tokenDecoder.decode(token) : null;
  });

  readonly isAuthenticated = computed(() => {
    const currentClaims = this.claims();
    return !!currentClaims && currentClaims.exp * 1000 > Date.now();
  });

  readonly currentRole = computed<UserRole | null>(() => this.claims()?.role ?? null);
  readonly tenantTimezone = computed<string>(
    () => this.claims()?.tenantTimezone ?? DEFAULT_TENANT_TIMEZONE,
  );

  get accessToken(): string | null {
    return this.tokenSignal();
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.httpClient
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, payload)
      .pipe(tap((response) => this.storeToken(response.accessToken)));
  }

  logout(): void {
    this.tokenStorage.clear();
    this.tokenSignal.set(null);
  }

  private storeToken(token: string): void {
    this.tokenStorage.write(token);
    this.tokenSignal.set(token);
  }
}
