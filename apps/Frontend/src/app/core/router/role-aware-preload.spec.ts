import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Route } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { describe, it, expect, vi, afterEach } from 'vitest';

import { RoleAwarePreloadStrategy } from './role-aware-preload';
import { AuthService } from '../auth/auth-service';
import { UserRole } from '../auth/jwt.model';

interface IdleGlobal {
  requestIdleCallback?: (callback: () => void, options?: { timeout: number }) => number;
  cancelIdleCallback?: (handle: number) => void;
}

const idleGlobal = globalThis as unknown as IdleGlobal;
const originalRequestIdle = idleGlobal.requestIdleCallback;
const originalCancelIdle = idleGlobal.cancelIdleCallback;

function makeStrategy(role: UserRole | null): RoleAwarePreloadStrategy {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      { provide: AuthService, useValue: { currentRole: () => role } },
    ],
  });
  return TestBed.inject(RoleAwarePreloadStrategy);
}

function routeWithRoles(roles?: UserRole[]): Route {
  return { data: roles ? { roles } : {} } as Route;
}

const load = (): Observable<string> => of('chunk');

afterEach(() => {
  idleGlobal.requestIdleCallback = originalRequestIdle;
  idleGlobal.cancelIdleCallback = originalCancelIdle;
  vi.useRealTimers();
});

describe('RoleAwarePreloadStrategy', () => {
  it('does not preload when there is no current role', () => {
    const strategy = makeStrategy(null);
    const loadSpy = vi.fn(load);
    let completed = false;

    strategy.preload(routeWithRoles(['Client']), loadSpy).subscribe({
      complete: () => (completed = true),
    });

    expect(loadSpy).not.toHaveBeenCalled();
    expect(completed).toBe(true);
  });

  it('does not preload when the route role is not allowed for the current role', () => {
    const strategy = makeStrategy('Student');
    const loadSpy = vi.fn(load);

    strategy.preload(routeWithRoles(['Client']), loadSpy).subscribe();

    expect(loadSpy).not.toHaveBeenCalled();
  });

  it('preloads via requestIdleCallback when the role is allowed', () => {
    idleGlobal.requestIdleCallback = vi.fn((callback: () => void) => {
      callback();
      return 7;
    });
    const strategy = makeStrategy('Client');
    const loadSpy = vi.fn(load);
    const values: unknown[] = [];

    strategy.preload(routeWithRoles(['Client']), loadSpy).subscribe((value) => values.push(value));

    expect(loadSpy).toHaveBeenCalled();
    expect(values).toEqual(['chunk']);
  });

  it('propagates a load error to the subscriber', () => {
    idleGlobal.requestIdleCallback = vi.fn((callback: () => void) => {
      callback();
      return 1;
    });
    const strategy = makeStrategy('Client');
    const failingLoad = (): Observable<string> => throwError(() => new Error('chunk failed'));
    let capturedError: unknown;

    strategy.preload(routeWithRoles(['Client']), failingLoad).subscribe({
      error: (error) => (capturedError = error),
    });

    expect(capturedError).toBeInstanceOf(Error);
  });

  it('preloads when the route declares no role restriction', () => {
    idleGlobal.requestIdleCallback = vi.fn((callback: () => void) => {
      callback();
      return 1;
    });
    const strategy = makeStrategy('Teacher');
    const loadSpy = vi.fn(load);

    strategy.preload(routeWithRoles(), loadSpy).subscribe();

    expect(loadSpy).toHaveBeenCalled();
  });

  it('falls back to setTimeout when requestIdleCallback is unavailable', () => {
    delete idleGlobal.requestIdleCallback;
    vi.useFakeTimers();
    const strategy = makeStrategy('Client');
    const loadSpy = vi.fn(load);

    strategy.preload(routeWithRoles(['Client']), loadSpy).subscribe();
    expect(loadSpy).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1_000);
    expect(loadSpy).toHaveBeenCalled();
  });

  it('cancels a pending idle callback when unsubscribed before it fires', () => {
    idleGlobal.requestIdleCallback = vi.fn(() => 42);
    const cancelSpy = vi.fn();
    idleGlobal.cancelIdleCallback = cancelSpy;
    const strategy = makeStrategy('Client');
    const loadSpy = vi.fn(load);

    const subscription = strategy.preload(routeWithRoles(['Client']), loadSpy).subscribe();
    subscription.unsubscribe();

    expect(cancelSpy).toHaveBeenCalledWith(42);
    expect(loadSpy).not.toHaveBeenCalled();
  });

  it('clears the fallback timeout when unsubscribed before it fires', () => {
    delete idleGlobal.requestIdleCallback;
    vi.useFakeTimers();
    const strategy = makeStrategy('Client');
    const loadSpy = vi.fn(load);

    const subscription = strategy.preload(routeWithRoles(['Client']), loadSpy).subscribe();
    subscription.unsubscribe();
    vi.advanceTimersByTime(2_000);

    expect(loadSpy).not.toHaveBeenCalled();
  });
});
