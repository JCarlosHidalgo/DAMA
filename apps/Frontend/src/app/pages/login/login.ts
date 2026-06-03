import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faEye, faEyeSlash } from '@fortawesome/free-solid-svg-icons';

import { AuthService } from '@core/auth';
import { defaultRouteForRole } from '@core/router';

import { ThemeToggle } from '@shared/components';

import { loginStyles } from './login.variants';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    FaIconComponent,
    ThemeToggle,
  ],
  templateUrl: './login.html',
  host: { class: 'block min-h-dvh bg-bg' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly styles = loginStyles();

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly faEye = faEye;
  readonly faEyeSlash = faEyeSlash;

  togglePasswordVisibility(): void {
    this.showPassword.update((isVisible) => !isVisible);
  }

  readonly form = this.formBuilder.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(5)]],
    password: ['', [Validators.required, Validators.minLength(5)]],
  });

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      await firstValueFrom(this.authService.login(this.form.getRawValue()));
      const role = this.authService.currentRole();
      if (
        (role === 'Teacher' || role === 'Student') &&
        this.authService.effectiveSubscriptionIndex() === 0
      ) {
        this.authService.clearSession();
        this.errorMessage.set('Tu escuela no tiene una suscripción vigente.');
        return;
      }
      const destinationUrl = role ? defaultRouteForRole(role) : '/yo';
      this.router.navigateByUrl(destinationUrl);
    } catch (error) {
      this.errorMessage.set(this.describeLoginError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private describeLoginError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'No se pudo conectar al servidor. Revisa tu conexión.';
      }
      if (error.status === 400 || error.status === 401) {
        return 'Credenciales inválidas';
      }
      if (error.status >= 500) {
        return 'Error del servidor. Intenta nuevamente en unos momentos.';
      }
    }
    return 'No se pudo iniciar sesión. Intenta nuevamente.';
  }
}
