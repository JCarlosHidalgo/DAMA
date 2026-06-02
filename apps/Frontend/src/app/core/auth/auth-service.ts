import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, map, shareReplay, tap, throwError } from 'rxjs';

import { environment } from '@env/environment';
import { JwtClaims, UserRole } from './jwt.model';
import { SessionStorageTokenStorage } from './token-storage';
import { TokenDecoder } from './token-decoder';

interface LoginRequest {
  username: string;
  password: string;
}

interface TokenPair {
  accessToken: string;
  refreshToken: string;
}

const DEFAULT_TENANT_TIMEZONE = 'America/La_Paz';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly httpClient = inject(HttpClient);
  private readonly tokenStorage = inject(SessionStorageTokenStorage);
  private readonly tokenDecoder = inject(TokenDecoder);

  private readonly tokenSignal = signal<string | null>(this.tokenStorage.read());
  private refreshInFlight: Observable<string> | null = null;

  private clockAnchorWallMs = Date.now();
  private clockAnchorPerfMs = performance.now();
  private readonly clockTick = signal(0);

  constructor() {
    setInterval(() => this.clockTick.update((value) => value + 1), 1000);
  }

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

  readonly effectiveSubscriptionIndex = computed<number>(() => {
    this.clockTick();
    const currentClaims = this.claims();
    if (!currentClaims) {
      return 0;
    }
    return this.serverNowMs() < currentClaims.subscriptionExpiresAt * 1000
      ? currentClaims.indexCoreServicesPyramid
      : 0;
  });

  get accessToken(): string | null {
    return this.tokenSignal();
  }

  login(payload: LoginRequest): Observable<TokenPair> {
    return this.httpClient
      .post<TokenPair>(`${environment.apiBaseUrl}/api/auth/login`, payload)
      .pipe(tap((pair) => this.storeTokens(pair)));
  }

  refreshAccessToken(): Observable<string> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }
    const refreshToken = this.tokenStorage.readRefresh();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }
    this.refreshInFlight = this.httpClient
      .post<TokenPair>(`${environment.apiBaseUrl}/api/auth/refresh`, { refreshToken })
      .pipe(
        tap((pair) => this.storeTokens(pair)),
        map((pair) => pair.accessToken),
        finalize(() => {
          this.refreshInFlight = null;
        }),
        shareReplay(1),
      );
    return this.refreshInFlight;
  }

  logout(): void {
    const token = this.accessToken;
    this.clearSession();
    if (token) {
      this.httpClient
        .post(
          `${environment.apiBaseUrl}/api/auth/logout`,
          {},
          { headers: { Authorization: `Bearer ${token}` } },
        )
        .subscribe({ error: () => undefined });
    }
  }

  clearSession(): void {
    this.tokenStorage.clear();
    this.tokenSignal.set(null);
    this.refreshInFlight = null;
  }

  private storeTokens(pair: TokenPair): void {
    this.tokenStorage.write(pair.accessToken);
    this.tokenStorage.writeRefresh(pair.refreshToken);
    this.tokenSignal.set(pair.accessToken);
    this.anchorServerClock();
  }

  private anchorServerClock(): void {
    const perfNow = performance.now();
    const wallNow = Date.now();
    const monotonicFloor = this.clockAnchorWallMs + (perfNow - this.clockAnchorPerfMs);
    this.clockAnchorWallMs = Math.max(wallNow, monotonicFloor);
    this.clockAnchorPerfMs = perfNow;
  }

  private serverNowMs(): number {
    return this.clockAnchorWallMs + (performance.now() - this.clockAnchorPerfMs);
  }
}
