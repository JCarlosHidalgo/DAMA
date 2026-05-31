import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { describe, it, expect, beforeEach } from 'vitest';

import { roleGuard } from './role-guard';
import { SessionStorageTokenStorage } from './token-storage';
import { UserRole } from './jwt.model';
import { InMemoryTokenStorage, buildJwtToken, buildJwtClaims } from '@testing';

function runRoleGuard(allowedRoles: UserRole[] | undefined): boolean | UrlTree {
  const route = {
    data: allowedRoles === undefined ? {} : { roles: allowedRoles },
  } as unknown as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;
  return TestBed.runInInjectionContext(() => roleGuard(route, state) as boolean | UrlTree);
}

describe('roleGuard', () => {
  function configure(role: UserRole | null): Router {
    const storage = new InMemoryTokenStorage(
      role === null ? null : buildJwtToken(buildJwtClaims({ role })),
    );
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SessionStorageTokenStorage, useValue: storage }],
    });
    return TestBed.inject(Router);
  }

  beforeEach(() => {
    sessionStorage.clear();
  });

  it('redirects to root when there is no current role', () => {
    const router = configure(null);

    const result = runRoleGuard(['Admin']);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/');
  });

  it('returns true when allowedRoles list is empty', () => {
    configure('Student');

    expect(runRoleGuard([])).toBe(true);
  });

  it('returns true when route.data has no roles key', () => {
    configure('Student');

    expect(runRoleGuard(undefined)).toBe(true);
  });

  it('returns true when the current role is in the allowed list', () => {
    configure('Teacher');

    expect(runRoleGuard(['Teacher', 'Admin'])).toBe(true);
  });

  it('redirects to /yo when the current role is not allowed', () => {
    const router = configure('Student');

    const result = runRoleGuard(['Admin']);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/yo');
  });
});
