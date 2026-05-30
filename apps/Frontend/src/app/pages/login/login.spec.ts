import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import { Login } from './login';
import { AuthService } from '../../core/auth/auth-service';
import { UserRole } from '../../core/auth/jwt.model';
import { signal } from '@angular/core';

interface AuthStub {
  login: ReturnType<typeof vi.fn>;
  currentRole: ReturnType<typeof signal<UserRole | null>>;
}

describe('Login', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Login>>;
  let component: Login;
  let authStub: AuthStub;
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.spyOn>;

  async function setUp(): Promise<void> {
    TestBed.resetTestingModule();
    authStub = {
      login: vi.fn(() => of({ accessToken: 'token' })),
      currentRole: signal<UserRole | null>(null),
    };
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: authStub },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
  }

  beforeEach(setUp);

  describe('form validation', () => {
    it('starts with an invalid form (both fields required, min length 5)', () => {
      expect(component.form.valid).toBe(false);
    });

    it('username with fewer than 5 chars is invalid', () => {
      component.form.controls.username.setValue('a');
      expect(component.form.controls.username.valid).toBe(false);
    });

    it('valid form when both fields have at least 5 chars', () => {
      component.form.setValue({ username: 'alice', password: 'secret' });
      expect(component.form.valid).toBe(true);
    });

    it('onSubmit on an invalid form marks fields as touched and does not call login', async () => {
      await component.onSubmit();
      expect(authStub.login).not.toHaveBeenCalled();
      expect(component.form.controls.username.touched).toBe(true);
      expect(component.form.controls.password.touched).toBe(true);
    });
  });

  describe('togglePasswordVisibility', () => {
    it('flips the showPassword signal', () => {
      expect(component.showPassword()).toBe(false);
      component.togglePasswordVisibility();
      expect(component.showPassword()).toBe(true);
      component.togglePasswordVisibility();
      expect(component.showPassword()).toBe(false);
    });
  });

  describe('successful login', () => {
    beforeEach(() => {
      component.form.setValue({ username: 'alice', password: 'secret' });
    });

    it('calls authService.login with form values', async () => {
      await component.onSubmit();
      expect(authStub.login).toHaveBeenCalledWith({ username: 'alice', password: 'secret' });
    });

    it('navigates to /yo/resumen for Client role', async () => {
      authStub.currentRole.set('Client');
      await component.onSubmit();
      expect(navigateSpy).toHaveBeenCalledWith('/yo/resumen');
    });

    it('navigates to /yo/resumen for Student role', async () => {
      authStub.currentRole.set('Student');
      await component.onSubmit();
      expect(navigateSpy).toHaveBeenCalledWith('/yo/resumen');
    });

    it('navigates to /yo/horario for Teacher role', async () => {
      authStub.currentRole.set('Teacher');
      await component.onSubmit();
      expect(navigateSpy).toHaveBeenCalledWith('/yo/horario');
    });

    it('navigates to / for Admin role', async () => {
      authStub.currentRole.set('Admin');
      await component.onSubmit();
      expect(navigateSpy).toHaveBeenCalledWith('/');
    });

    it('navigates to /yo when no role is present', async () => {
      authStub.currentRole.set(null);
      await component.onSubmit();
      expect(navigateSpy).toHaveBeenCalledWith('/yo');
    });

    it('flips loading on then off', async () => {
      let loadingDuringRequest: boolean | undefined;
      authStub.login.mockImplementationOnce(() => {
        loadingDuringRequest = component.loading();
        return of({ accessToken: 't' });
      });
      authStub.currentRole.set('Client');
      await component.onSubmit();

      expect(loadingDuringRequest).toBe(true);
      expect(component.loading()).toBe(false);
    });

    it('clears any prior errorMessage on submit start', async () => {
      component.errorMessage.set('previous error');
      authStub.currentRole.set('Client');
      await component.onSubmit();
      expect(component.errorMessage()).toBeNull();
    });
  });

  describe('login errors', () => {
    beforeEach(() => {
      component.form.setValue({ username: 'alice', password: 'secret' });
    });

    it('sets a connection error message on status 0', async () => {
      authStub.login.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })));
      await component.onSubmit();
      expect(component.errorMessage()).toContain('conectar');
      expect(component.loading()).toBe(false);
    });

    it.each([400, 401])('sets "Credenciales inválidas" on status %i', async (status) => {
      authStub.login.mockReturnValueOnce(
        throwError(() => new HttpErrorResponse({ status })),
      );
      await component.onSubmit();
      expect(component.errorMessage()).toBe('Credenciales inválidas');
    });

    it.each([500, 502, 503])('sets a server-error message on status %i', async (status) => {
      authStub.login.mockReturnValueOnce(
        throwError(() => new HttpErrorResponse({ status })),
      );
      await component.onSubmit();
      expect(component.errorMessage()).toContain('servidor');
    });

    it('falls back to a generic message on non-Http errors', async () => {
      authStub.login.mockReturnValueOnce(throwError(() => new Error('boom')));
      await component.onSubmit();
      expect(component.errorMessage()).toContain('No se pudo iniciar sesión');
    });

    it('falls back to generic on Http status that doesn\'t match the known buckets (e.g. 404)', async () => {
      authStub.login.mockReturnValueOnce(
        throwError(() => new HttpErrorResponse({ status: 404 })),
      );
      await component.onSubmit();
      expect(component.errorMessage()).toContain('No se pudo iniciar sesión');
    });
  });
});
