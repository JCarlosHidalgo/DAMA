import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { AuthService } from './auth-service';
import { SessionStorageTokenStorage } from './token-storage';
import { InMemoryTokenStorage, buildJwtClaims, buildJwtToken } from '@testing';
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
    it('posts credentials and stores the returned access token', () => {
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
      pendingRequest.flush({ accessToken: issuedToken });

      expect(receivedToken).toBe(issuedToken);
      expect(authService.accessToken).toBe(issuedToken);
      expect(inMemoryStorage.read()).toBe(issuedToken);
      expect(authService.currentRole()).toBe('Client');
    });
  });

  describe('logout', () => {
    it('clears storage and the token signal', () => {
      const token = buildJwtToken();
      const authService = instantiate(token);

      authService.logout();

      expect(authService.accessToken).toBeNull();
      expect(inMemoryStorage.read()).toBeNull();
      expect(authService.isAuthenticated()).toBe(false);
      expect(authService.currentRole()).toBeNull();
    });
  });
});
