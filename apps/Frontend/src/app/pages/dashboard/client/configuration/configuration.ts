import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PaymentApi, AuthApi } from '@core/api';
import { AuthService } from '@core/auth';
import { TodotixAppKeyStatus } from '@core/models';
import { NotificationService } from '@core/services';
import { PageHead } from '@shared/components';

type AppKeyState =
  | { kind: 'loading' }
  | { kind: 'ready'; status: TodotixAppKeyStatus }
  | { kind: 'error' };

const TIMEZONE_OPTIONS = [
  'America/La_Paz',
  'America/Lima',
  'America/Bogota',
  'America/Mexico_City',
  'America/Argentina/Buenos_Aires',
  'America/Sao_Paulo',
  'America/Santiago',
  'America/New_York',
  'America/Los_Angeles',
  'Europe/Madrid',
  'Europe/London',
  'Europe/Paris',
  'Asia/Tokyo',
] as const;

const APP_KEY_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

@Component({
  selector: 'app-client-configuration',
  imports: [
    PageHead,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <app-page-head title="Configuración" subtitle="Zona horaria y credenciales de cobro" />

    <section class="config-card">
      <h2 class="t-title-sm">Zona horaria</h2>
      <p class="t-body-sm hint">Aplica a todas las fechas mostradas en tu cuenta.</p>
      <mat-form-field appearance="outline" class="field">
        <mat-label>Zona horaria</mat-label>
        <mat-select [value]="selectedTimezone()" (selectionChange)="onTimezoneChange($event.value)">
          @for (zone of timezoneOptions; track zone) {
            <mat-option [value]="zone">{{ zone }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </section>

    <section class="config-card">
      <h2 class="t-title-sm">App-key de Todotix</h2>
      <p class="t-body-sm hint">
        Clave de tu canal de cobro Todotix. Si no defines una propia, se usa la del sistema.
      </p>

      @switch (appKeyState().kind) {
        @case ('loading') {
          <p class="t-body-sm">Cargando…</p>
        }
        @case ('error') {
          <p class="t-body-sm error-text">No se pudo cargar la app-key.</p>
        }
        @case ('ready') {
          @if (asReady(appKeyState()); as state) {
            <div class="status-row">
              <span class="t-label-up">Estado</span>
              <span class="t-body-md">
                {{ state.status.hasCustomKey ? 'Personalizada' : 'Usando la del sistema' }}
              </span>
            </div>
            <div class="status-row">
              <span class="t-label-up">App-key</span>
              <span class="t-body-md key-value">
                {{ revealedKey() ?? state.status.maskedAppKey ?? '—' }}
              </span>
              <button mat-stroked-button type="button" (click)="toggleReveal()">
                {{ revealedKey() ? 'Ocultar' : 'Mostrar' }}
              </button>
            </div>
            @if (state.status.updatedAt) {
              <div class="status-row">
                <span class="t-label-up">Última actualización</span>
                <span class="t-body-md">{{ state.status.updatedAt }}</span>
              </div>
            }
          }

          <form [formGroup]="form" (ngSubmit)="saveAppKey()" class="edit-form">
            <mat-form-field appearance="outline" class="field">
              <mat-label>Nueva app-key</mat-label>
              <input
                matInput
                formControlName="appKey"
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
  `,
  styles: `
    :host {
      display: block;
    }
    .config-card {
      margin-bottom: var(--dama-space-5);
      padding: var(--dama-space-4) var(--dama-space-5);
      background: var(--dama-surface);
      border: 1px solid var(--dama-border);
      border-radius: var(--dama-radius-md);
      box-shadow: var(--dama-shadow-xs);
    }
    .hint {
      color: var(--dama-text-muted);
      margin: 4px 0 var(--dama-space-4);
    }
    .field {
      min-width: 320px;
    }
    .status-row {
      display: flex;
      align-items: center;
      gap: var(--dama-space-4);
      margin-bottom: var(--dama-space-3);
    }
    .key-value {
      font-variant-numeric: tabular-nums;
    }
    .error-text {
      color: var(--dama-error, #b3261e);
    }
    .edit-form {
      display: flex;
      align-items: flex-start;
      gap: var(--dama-space-4);
      margin-top: var(--dama-space-4);
      flex-wrap: wrap;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientConfiguration {
  private readonly paymentApi = inject(PaymentApi);
  private readonly authApi = inject(AuthApi);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly formBuilder = inject(FormBuilder);

  private readonly tenantId = this.authService.claims()?.tenantId ?? '';

  protected readonly timezoneOptions = TIMEZONE_OPTIONS;
  protected readonly selectedTimezone = signal(this.authService.tenantTimezone());

  readonly appKeyState = signal<AppKeyState>({ kind: 'loading' });
  readonly revealedKey = signal<string | null>(null);
  readonly saving = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    appKey: ['', [Validators.required, Validators.pattern(APP_KEY_PATTERN)]],
  });

  constructor() {
    this.loadAppKeyStatus();
  }

  protected asReady(state: AppKeyState): { status: TodotixAppKeyStatus } | null {
    return state.kind === 'ready' ? state : null;
  }

  onTimezoneChange(timezone: string): void {
    if (timezone === this.selectedTimezone()) {
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

  private loadAppKeyStatus(): void {
    this.appKeyState.set({ kind: 'loading' });
    this.paymentApi.getTodotixAppKeyStatus().subscribe({
      next: (status) => this.appKeyState.set({ kind: 'ready', status }),
      error: () => this.appKeyState.set({ kind: 'error' }),
    });
  }
}
