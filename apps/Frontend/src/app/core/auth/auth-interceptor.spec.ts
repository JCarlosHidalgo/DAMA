import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { authInterceptor } from './auth-interceptor';
import { AuthService } from './auth-service';
import { SessionStorageTokenStorage } from './token-storage';
import { InMemoryTokenStorage } from '../../../testing/mocks/token-storage.mock';
import { buildJwtToken } from '../../../testing/fixtures/jwt-tokens.fixture';
import { buildJwtClaims } from '../../../testing/builders/jwt-claims.builder';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpController: HttpTestingController;
  let storage: InMemoryTokenStorage;

  function configure(initialToken: string | null = null): void {
    storage = new InMemoryTokenStorage(initialToken);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: SessionStorageTokenStorage, useValue: storage },
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    httpController = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    httpController.verify();
  });

  it('does not add Authorization header when there is no token', () => {
    configure(null);

    httpClient.get('/api/anything').subscribe();

    const pending = httpController.expectOne('/api/anything');
    expect(pending.request.headers.has('Authorization')).toBe(false);
    pending.flush({});
  });

  it('attaches Authorization: Bearer <token> when a token is present', () => {
    const token = buildJwtToken(buildJwtClaims());
    configure(token);

    httpClient.get('/api/anything').subscribe();

    const pending = httpController.expectOne('/api/anything');
    expect(pending.request.headers.get('Authorization')).toBe(`Bearer ${token}`);
    pending.flush({});
  });

  describe('on 401 responses', () => {
    let router: Router;
    let navigateSpy: ReturnType<typeof vi.spyOn>;

    beforeEach(() => {
      configure(buildJwtToken(buildJwtClaims()));
      router = TestBed.inject(Router);
      navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    });

    it('logs out and navigates to root, then propagates the error', () => {
      let capturedStatus: number | undefined;
      httpClient.get('/api/secured').subscribe({
        next: () => {
          throw new Error('should not emit value');
        },
        error: (error) => {
          capturedStatus = error.status;
        },
      });

      const pending = httpController.expectOne('/api/secured');
      pending.flush({ message: 'nope' }, { status: 401, statusText: 'Unauthorized' });

      const authService = TestBed.inject(AuthService);
      expect(capturedStatus).toBe(401);
      expect(authService.accessToken).toBeNull();
      expect(storage.read()).toBeNull();
      expect(navigateSpy).toHaveBeenCalledWith('/');
    });
  });

  describe('on non-401 errors', () => {
    let router: Router;
    let navigateSpy: ReturnType<typeof vi.spyOn>;

    beforeEach(() => {
      configure(buildJwtToken(buildJwtClaims()));
      router = TestBed.inject(Router);
      navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    });

    it('does not log out and propagates the error', () => {
      let capturedStatus: number | undefined;
      httpClient.get('/api/whatever').subscribe({
        next: () => undefined,
        error: (error) => {
          capturedStatus = error.status;
        },
      });

      const pending = httpController.expectOne('/api/whatever');
      pending.flush({ message: 'oops' }, { status: 500, statusText: 'Server Error' });

      expect(capturedStatus).toBe(500);
      expect(navigateSpy).not.toHaveBeenCalled();
      expect(storage.read()).not.toBeNull();
    });
  });
});
