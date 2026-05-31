import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { describe, it, expect, beforeEach } from 'vitest';

import { authGuard } from './auth-guard';
import { AuthService } from './auth-service';
import { SessionStorageTokenStorage } from './token-storage';
import { InMemoryTokenStorage, buildJwtToken, buildJwtClaims } from '@testing';

function runAuthGuard(): boolean | UrlTree {
  const dummyRoute = {} as ActivatedRouteSnapshot;
  const dummyState = {} as RouterStateSnapshot;
  return TestBed.runInInjectionContext(
    () => authGuard(dummyRoute, dummyState) as boolean | UrlTree,
  );
}

describe('authGuard', () => {
  function configure(initialToken: string | null): {
    router: Router;
    storage: InMemoryTokenStorage;
  } {
    const storage = new InMemoryTokenStorage(initialToken);
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SessionStorageTokenStorage, useValue: storage }],
    });
    return { router: TestBed.inject(Router), storage };
  }

  beforeEach(() => {
    sessionStorage.clear();
  });

  it('returns true when the user is authenticated', () => {
    const token = buildJwtToken(buildJwtClaims());
    configure(token);

    expect(runAuthGuard()).toBe(true);
  });

  it('logs out and returns a redirect UrlTree to root when not authenticated', () => {
    const { router, storage } = configure(null);

    const result = runAuthGuard();

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/');
    expect(storage.read()).toBeNull();
  });

  it('logs out when the token is expired and returns a UrlTree', () => {
    const expired = buildJwtToken(buildJwtClaims({ exp: Math.floor(Date.now() / 1000) - 10 }));
    const { storage } = configure(expired);
    expect(storage.read()).toBe(expired);

    const result = runAuthGuard();

    expect(result).toBeInstanceOf(UrlTree);
    expect(storage.read()).toBeNull();
    const authService = TestBed.inject(AuthService);
    expect(authService.isAuthenticated()).toBe(false);
  });
});
