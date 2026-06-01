import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { Dashboard } from './dashboard';
import { AuthService, JwtClaims, UserRole } from '@core/auth';
import { buildJwtClaims } from '@testing';

interface AuthStub {
  currentRole: ReturnType<typeof signal<UserRole | null>>;
  claims: ReturnType<typeof signal<JwtClaims | null>>;
  logout: ReturnType<typeof vi.fn>;
}

interface BreakpointStub {
  isMatched: ReturnType<typeof vi.fn>;
  observe: ReturnType<typeof vi.fn>;
}

describe('Dashboard', () => {
  let authStub: AuthStub;
  let breakpointStub: BreakpointStub;
  let breakpointSubject: Subject<{ matches: boolean }>;
  let fixture: ReturnType<typeof TestBed.createComponent<Dashboard>>;
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.spyOn>;

  async function setUp(role: UserRole | null, isHandsetInitial = false): Promise<void> {
    TestBed.resetTestingModule();
    authStub = {
      currentRole: signal<UserRole | null>(role),
      claims: signal<JwtClaims | null>(
        role ? buildJwtClaims({ role, userName: 'someone@example.com' }) : null,
      ),
      logout: vi.fn(),
    };
    breakpointSubject = new Subject();
    breakpointStub = {
      isMatched: vi.fn(() => isHandsetInitial),
      observe: vi.fn(() => breakpointSubject.asObservable()),
    };

    await TestBed.configureTestingModule({
      imports: [Dashboard, NoopAnimationsModule],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: authStub },
        { provide: BreakpointObserver, useValue: breakpointStub },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(Dashboard);
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
  }

  describe('tab list by role', () => {
    it('shows 7 tabs for Client', async () => {
      await setUp('Client');
      expect(fixture.componentInstance.tabs()).toHaveLength(7);
      expect(fixture.componentInstance.tabs()[0].path).toBe('resumen');
    });

    it('shows 1 tab for Teacher', async () => {
      await setUp('Teacher');
      expect(fixture.componentInstance.tabs()).toHaveLength(1);
      expect(fixture.componentInstance.tabs()[0].path).toBe('horario');
    });

    it('shows 6 tabs for Student', async () => {
      await setUp('Student');
      expect(fixture.componentInstance.tabs()).toHaveLength(6);
    });

    it('shows 1 tab for Admin', async () => {
      await setUp('Admin');
      expect(fixture.componentInstance.tabs()).toHaveLength(1);
      expect(fixture.componentInstance.tabs()[0].path).toBe('tenants');
    });

    it('shows 0 tabs when there is no role', async () => {
      await setUp(null);
      expect(fixture.componentInstance.tabs()).toHaveLength(0);
    });
  });

  describe('displayName', () => {
    it('returns the userName from claims', async () => {
      await setUp('Client');
      expect(fixture.componentInstance.displayName()).toBe('someone@example.com');
    });

    it('returns an empty string when no claims', async () => {
      await setUp(null);
      expect(fixture.componentInstance.displayName()).toBe('');
    });
  });

  describe('breakpoint reactivity', () => {
    it('starts non-handset and expanded when isMatched is false', async () => {
      await setUp('Client', false);
      expect(fixture.componentInstance.isHandset()).toBe(false);
      expect(fixture.componentInstance.expanded()).toBe(true);
    });

    it('starts handset and collapsed when isMatched is true', async () => {
      await setUp('Client', true);
      expect(fixture.componentInstance.isHandset()).toBe(true);
      expect(fixture.componentInstance.expanded()).toBe(false);
    });

    it('collapses expanded sidebar when breakpoint flips to handset', async () => {
      await setUp('Client', false);
      expect(fixture.componentInstance.expanded()).toBe(true);

      breakpointSubject.next({ matches: true });
      expect(fixture.componentInstance.isHandset()).toBe(true);
      expect(fixture.componentInstance.expanded()).toBe(false);
    });

    it('keeps expanded false when leaving handset (no automatic re-expand)', async () => {
      await setUp('Client', true);
      breakpointSubject.next({ matches: false });
      expect(fixture.componentInstance.isHandset()).toBe(false);
    });
  });

  describe('toggleSidenav', () => {
    it('toggles the sidenav element when in handset mode', async () => {
      await setUp('Client', true);
      const toggleSpy = vi
        .spyOn(fixture.componentInstance.sidenav(), 'toggle')
        .mockResolvedValue('open' as never);

      fixture.componentInstance.toggleSidenav();

      expect(toggleSpy).toHaveBeenCalledTimes(1);
    });

    it('flips expanded when not in handset mode', async () => {
      await setUp('Client', false);
      expect(fixture.componentInstance.expanded()).toBe(true);

      fixture.componentInstance.toggleSidenav();
      expect(fixture.componentInstance.expanded()).toBe(false);

      fixture.componentInstance.toggleSidenav();
      expect(fixture.componentInstance.expanded()).toBe(true);
    });
  });

  describe('onLogout', () => {
    it('clears auth and navigates to /', async () => {
      await setUp('Client');
      fixture.componentInstance.onLogout();
      expect(authStub.logout).toHaveBeenCalledTimes(1);
      expect(navigateSpy).toHaveBeenCalledWith('/');
    });
  });
});
