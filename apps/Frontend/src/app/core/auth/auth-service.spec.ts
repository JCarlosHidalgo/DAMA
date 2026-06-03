import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { AuthService } from './auth-service';
import { SessionStorageTokenStorage } from './token-storage';
import {
  InMemoryTokenStorage,
  buildJwtClaims,
  buildJwtToken,
  buildRawPayloadToken,
} from '@testing';
import { environment } from '@env/environment';

describe('AuthService', () => {
  let inMemoryStorage: InMemoryTokenStorage;
  let httpController: HttpTestingController;

  function instantiate(initialToken: string | null = null): AuthService {
    inMemoryStorage = new InMemoryTokenStorage(initialToken);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SessionStorageTokenStorage, useValue: inMemoryStorage },
      ],
    });
    httpController = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  afterEach(() => {
    if (httpController) {
      httpController.verify();
    }
  });

  describe('initial state', () => {
    beforeEach(() => {
      instantiate();
    });

    it('starts with no token', () => {
      const authService = TestBed.inject(AuthService);
      expect(authService.accessToken).toBeNull();
    });

    it('reports unauthenticated when there is no token', () => {
      const authService = TestBed.inject(AuthService);
      expect(authService.isAuthenticated()).toBe(false);
      expect(authService.claims()).toBeNull();
      expect(authService.currentRole()).toBeNull();
    });

    it('falls back to America/La_Paz when no claims are present', () => {
      const authService = TestBed.inject(AuthService);
      expect(authService.tenantTimezone()).toBe('America/La_Paz');
    });
  });

  describe('with a token already in storage', () => {
    it('exposes decoded claims as signals', () => {
      const claims = buildJwtClaims({ role: 'Teacher', tenantTimezone: 'America/Mexico_City' });
      const token = buildJwtToken(claims);

      const authService = instantiate(token);

      expect(authService.accessToken).toBe(token);
      expect(authService.claims()?.userName).toBe(claims.userName);
      expect(authService.currentRole()).toBe('Teacher');
      expect(authService.tenantTimezone()).toBe('America/Mexico_City');
      expect(authService.isAuthenticated()).toBe(true);
    });

    it('reports unauthenticated when token is expired', () => {
      const expiredClaims = buildJwtClaims({
        exp: Math.floor(Date.now() / 1000) - 60,
      });
      const token = buildJwtToken(expiredClaims);

      const authService = instantiate(token);

      expect(authService.isAuthenticated()).toBe(false);
    });
  });

  describe('login', () => {
    it('posts credentials and stores the returned token pair', () => {
      const authService = instantiate();
      const futureClaims = buildJwtClaims({ role: 'Client' });
      const issuedToken = buildJwtToken(futureClaims);

      let receivedToken: string | undefined;
      authService
        .login({ username: 'admin@example.com', password: 'secret' })
        .subscribe((response) => {
          receivedToken = response.accessToken;
        });

      const pendingRequest = httpController.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
      expect(pendingRequest.request.method).toBe('POST');
      expect(pendingRequest.request.body).toEqual({
        username: 'admin@example.com',
        password: 'secret',
      });
      pendingRequest.flush({ accessToken: issuedToken, refreshToken: 'refresh-abc' });

      expect(receivedToken).toBe(issuedToken);
      expect(authService.accessToken).toBe(issuedToken);
      expect(inMemoryStorage.read()).toBe(issuedToken);
      expect(inMemoryStorage.readRefresh()).toBe('refresh-abc');
      expect(authService.currentRole()).toBe('Client');
    });
  });

  describe('refreshAccessToken', () => {
    it('posts the stored refresh token and stores the rotated pair', () => {
      const initial = buildJwtToken(buildJwtClaims({ role: 'Client' }));
      inMemoryStorage = new InMemoryTokenStorage(initial, 'refresh-old');
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          { provide: SessionStorageTokenStorage, useValue: inMemoryStorage },
        ],
      });
      httpController = TestBed.inject(HttpTestingController);
      const authService = TestBed.inject(AuthService);
      const rotated = buildJwtToken(buildJwtClaims({ role: 'Client' }));

      let emitted: string | undefined;
      authService.refreshAccessToken().subscribe((accessToken) => {
        emitted = accessToken;
      });

      const pending = httpController.expectOne(`${environment.apiBaseUrl}/api/auth/refresh`);
      expect(pending.request.body).toEqual({ refreshToken: 'refresh-old' });
      pending.flush({ accessToken: rotated, refreshToken: 'refresh-new' });

      expect(emitted).toBe(rotated);
      expect(authService.accessToken).toBe(rotated);
      expect(inMemoryStorage.readRefresh()).toBe('refresh-new');
    });

    it('errors without an HTTP call when no refresh token is stored', () => {
      const authService = instantiate(buildJwtToken(buildJwtClaims()));

      let errored = false;
      authService.refreshAccessToken().subscribe({ error: () => (errored = true) });

      expect(errored).toBe(true);
    });

    it('returns the in-flight observable instead of issuing a second request', () => {
      const initial = buildJwtToken(buildJwtClaims({ role: 'Client' }));
      inMemoryStorage = new InMemoryTokenStorage(initial, 'refresh-old');
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          { provide: SessionStorageTokenStorage, useValue: inMemoryStorage },
        ],
      });
      httpController = TestBed.inject(HttpTestingController);
      const authService = TestBed.inject(AuthService);
      const rotated = buildJwtToken(buildJwtClaims({ role: 'Client' }));

      const firstCall = authService.refreshAccessToken();
      const secondCall = authService.refreshAccessToken();
      expect(secondCall).toBe(firstCall);

      let firstEmit: string | undefined;
      let secondEmit: string | undefined;
      firstCall.subscribe((accessToken) => (firstEmit = accessToken));
      secondCall.subscribe((accessToken) => (secondEmit = accessToken));

      const pending = httpController.expectOne(`${environment.apiBaseUrl}/api/auth/refresh`);
      pending.flush({ accessToken: rotated, refreshToken: 'refresh-new' });

      expect(firstEmit).toBe(rotated);
      expect(secondEmit).toBe(rotated);
    });
  });

  describe('effectiveSubscriptionIndex', () => {
    it('is 0 when there are no claims', () => {
      const authService = instantiate();
      expect(authService.effectiveSubscriptionIndex()).toBe(0);
    });

    it('exposes the pyramid index while the subscription is active', () => {
      const nowSeconds = Math.floor(Date.now() / 1000);
      const token = buildRawPayloadToken({
        role: 'Client',
        exp: nowSeconds + 60 * 60,
        index_core_services_pyramid: 3,
        subscription_expires_at: nowSeconds + 60 * 60,
      });

      const authService = instantiate(token);

      expect(authService.effectiveSubscriptionIndex()).toBe(3);
    });

    it('falls back to 0 once the subscription has expired', () => {
      const nowSeconds = Math.floor(Date.now() / 1000);
      const token = buildRawPayloadToken({
        role: 'Client',
        exp: nowSeconds + 60 * 60,
        index_core_services_pyramid: 3,
        subscription_expires_at: nowSeconds - 60 * 60,
      });

      const authService = instantiate(token);

      expect(authService.effectiveSubscriptionIndex()).toBe(0);
    });
  });

  describe('logout', () => {
    it('clears the session and revokes the refresh tokens server-side', () => {
      const token = buildJwtToken();
      const authService = instantiate(token);

      authService.logout();

      expect(authService.accessToken).toBeNull();
      expect(inMemoryStorage.read()).toBeNull();
      expect(authService.isAuthenticated()).toBe(false);
      expect(authService.currentRole()).toBeNull();

      const pending = httpController.expectOne(`${environment.apiBaseUrl}/api/auth/logout`);
      expect(pending.request.method).toBe('POST');
      expect(pending.request.headers.get('Authorization')).toBe(`Bearer ${token}`);
      pending.flush({});
    });
  });
});
