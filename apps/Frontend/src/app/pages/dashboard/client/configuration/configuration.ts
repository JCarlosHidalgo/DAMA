import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PaymentApi, AuthApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationService } from '@core/services';
import { PageHead } from '@shared/components';
import { NoPasswordManager } from '@shared/directives';

import {
  AppKeyState,
  asReadyAppKey,
  shouldUpdateTimezone,
  subscriptionAllowsTodotix,
  TIMEZONE_OPTIONS,
} from './configuration.logic';
import { appKeyValidator } from './configuration.validators';
import { clientConfigurationStyles } from './configuration.variants';

@Component({
  selector: 'app-client-configuration',
  imports: [
    PageHead,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    NoPasswordManager,
  ],
  template: `
    <app-page-head title="Configuración" subtitle="Zona horaria y credenciales de cobro" />

    <section [class]="styles.card()">
      <h2 class="t-title-sm">Zona horaria</h2>
      <p [class]="styles.hint()">Aplica a todas las fechas mostradas en tu cuenta.</p>
      <mat-form-field appearance="outline" [class]="styles.field()">
        <mat-label>Zona horaria</mat-label>
        <mat-select [value]="selectedTimezone()" (selectionChange)="onTimezoneChange($event.value)">
          @for (zone of timezoneOptions; track zone) {
            <mat-option [value]="zone">{{ zone }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </section>

    @if (canManageTodotix()) {
      <section [class]="styles.card()">
        <h2 class="t-title-sm">App-key de Todotix</h2>
        <p [class]="styles.hint()">
          Clave de tu canal de cobro Todotix. Sin una credencial configurada y válida no podrás
          recibir pagos.
        </p>

        @switch (appKeyState().kind) {
          @case ('loading') {
            <p class="t-body-sm">Cargando…</p>
          }
          @case ('error') {
            <p [class]="styles.errorText()">No se pudo cargar la app-key.</p>
          }
          @case ('ready') {
            @if (asReady(appKeyState()); as state) {
              <div [class]="styles.statusRow()">
                <span class="t-label-up">Estado</span>
                <span class="t-body-md">
                  {{ state.status.hasCustomKey ? 'Configurada' : 'No configurada' }}
                </span>
              </div>
              <div [class]="styles.statusRow()">
                <span class="t-label-up">App-key</span>
                <span [class]="styles.keyValue()">
                  {{ revealedKey() ?? state.status.maskedAppKey ?? '—' }}
                </span>
                <button mat-stroked-button type="button" (click)="toggleReveal()">
                  {{ revealedKey() ? 'Ocultar' : 'Mostrar' }}
                </button>
              </div>
              @if (state.status.hasCustomKey) {
                <div [class]="styles.statusRow()">
                  <button
                    mat-stroked-button
                    type="button"
                    [disabled]="testing()"
                    (click)="testCredential()"
                  >
                    Probar Credencial
                  </button>
                </div>
              }
            }

            <form [formGroup]="form" (ngSubmit)="saveAppKey()" [class]="styles.editForm()">
              <mat-form-field appearance="outline" [class]="styles.field()">
                <mat-label>Nueva app-key</mat-label>
                <input
                  matInput
                  formControlName="appKey"
                  autocomplete="off"
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
                @if (form.controls.appKey.invalid && form.controls.appKey.touched) {
                  <mat-error>Debe ser un GUID válido (formato 8-4-4-4-12 en minúsculas).</mat-error>
                }
              </mat-form-field>
              <button
                mat-flat-button
                color="primary"
                type="submit"
                [disabled]="form.invalid || saving()"
              >
                Guardar app-key
              </button>
            </form>
          }
        }
      </section>
    } @else {
      <section [class]="styles.card()">
        <h2 class="t-title-sm">App-key de Todotix</h2>
        <p [class]="styles.hint()">
          Disponible al activar la gestión de pagos (suscripción nivel 3).
        </p>
      </section>
    }
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientConfiguration {
  private readonly paymentApi = inject(PaymentApi);
  private readonly authApi = inject(AuthApi);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly styles = clientConfigurationStyles();

  private readonly tenantId = this.authService.claims()?.tenantId ?? '';

  protected readonly timezoneOptions = TIMEZONE_OPTIONS;
  protected readonly selectedTimezone = signal(this.authService.tenantTimezone());
  protected readonly canManageTodotix = computed(() =>
    subscriptionAllowsTodotix(this.authService.effectiveSubscriptionIndex()),
  );

  readonly appKeyState = signal<AppKeyState>({ kind: 'loading' });
  readonly revealedKey = signal<string | null>(null);
  readonly saving = signal(false);
  readonly testing = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    appKey: ['', [Validators.required, appKeyValidator]],
  });

  constructor() {
    if (this.canManageTodotix()) {
      this.loadAppKeyStatus();
    }
  }

  protected asReady(state: AppKeyState): ReturnType<typeof asReadyAppKey> {
    return asReadyAppKey(state);
  }

  onTimezoneChange(timezone: string): void {
    if (!shouldUpdateTimezone(timezone, this.selectedTimezone())) {
      return;
    }
    this.authApi.updateTenantTimezone(this.tenantId, { timezone }).subscribe({
      next: () => {
        this.selectedTimezone.set(timezone);
        this.notifications.success('Zona horaria actualizada.');
      },
      error: () => this.notifications.error('No se pudo actualizar la zona horaria.'),
    });
  }

  toggleReveal(): void {
    if (this.revealedKey()) {
      this.revealedKey.set(null);
      return;
    }
    this.paymentApi.revealTodotixAppKey().subscribe({
      next: (reveal) => this.revealedKey.set(reveal.appKey),
      error: () => this.notifications.error('No se pudo mostrar la app-key.'),
    });
  }

  saveAppKey(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }
    this.saving.set(true);
    const appKey = this.form.getRawValue().appKey;
    this.paymentApi.updateTodotixAppKey({ appKey }).subscribe({
      next: () => {
        this.notifications.success('App-key actualizada.');
        this.form.reset({ appKey: '' });
        this.revealedKey.set(null);
        this.saving.set(false);
        this.loadAppKeyStatus();
      },
      error: () => {
        this.notifications.error('No se pudo actualizar la app-key.');
        this.saving.set(false);
      },
    });
  }

  testCredential(): void {
    if (this.testing()) {
      return;
    }
    this.testing.set(true);
    this.paymentApi.testTodotixCredential().subscribe({
      next: () => {
        this.notifications.success('La credencial funciona');
        this.testing.set(false);
      },
      error: () => {
        this.notifications.error('La credencial no funciona.');
        this.testing.set(false);
      },
    });
  }

  private loadAppKeyStatus(): void {
    this.appKeyState.set({ kind: 'loading' });
    this.paymentApi.getTodotixAppKeyStatus().subscribe({
      next: (status) => this.appKeyState.set({ kind: 'ready', status }),
      error: () => this.appKeyState.set({ kind: 'error' }),
    });
  }
}
