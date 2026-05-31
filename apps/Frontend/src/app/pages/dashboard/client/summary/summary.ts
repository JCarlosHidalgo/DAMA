import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of, startWith } from 'rxjs';

import { AuthApi, PaymentApi } from '@core/api';
import { AuthService } from '@core/auth';
import { PaymentSummary } from '@core/models';
import { NotificationService } from '@core/services';
import { ErrorState, LoadingSkeleton, PageHead, StatCard } from '@shared/components';
import { MoneyPipe, TenantDatePipe } from '@shared/pipes';

type SummaryState =
  | { kind: 'loading' }
  | { kind: 'ready'; data: PaymentSummary }
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

@Component({
  selector: 'app-client-summary',
  imports: [
    PageHead,
    StatCard,
    LoadingSkeleton,
    ErrorState,
    MoneyPipe,
    TenantDatePipe,
    MatFormFieldModule,
    MatSelectModule,
  ],
  template: `
    <app-page-head title="Resumen" subtitle="Ganancias y actividad reciente" />

    <mat-form-field appearance="outline" class="tz-field">
      <mat-label>Zona horaria</mat-label>
      <mat-select [value]="selectedTimezone()" (selectionChange)="onTimezoneChange($event.value)">
        @for (zone of timezoneOptions; track zone) {
          <mat-option [value]="zone">{{ zone }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    @switch (state().kind) {
      @case ('loading') {
        <div class="kpi-grid">
          <app-loading-skeleton [height]="120" />
          <app-loading-skeleton [height]="120" />
          <app-loading-skeleton [height]="120" />
          <app-loading-skeleton [height]="64" width="100%" />
        </div>
      }
      @case ('error') {
        <app-error-state message="No se pudo cargar el resumen." />
      }
      @case ('ready') {
        @if (ready(); as summary) {
          <div class="kpi-grid">
            <app-stat-card
              label="Ganancias totales"
              [value]="summary.totalEarnings | money"
              icon="money-bill"
            />
            <app-stat-card
              label="Últimos 30 días"
              [value]="summary.monthEarnings | money"
              icon="credit-card"
            />
            <app-stat-card
              label="Primer pago"
              [value]="summary.firstPaymentDate | tenantDate"
              icon="calendar"
            />
            <div class="range-card">
              <span class="t-label-up">Rango consultado</span>
              <span class="range-value t-body-md">
                {{ summary.from | tenantDate }} → {{ summary.to | tenantDate }}
              </span>
            </div>
          </div>
        }
      }
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .tz-field {
      margin-bottom: var(--dama-space-4);
      min-width: 260px;
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: var(--dama-space-4);
    }
    .range-card {
      grid-column: 1 / -1;
      display: flex;
      flex-direction: column;
      gap: 6px;
      padding: var(--dama-space-4) var(--dama-space-5);
      background: var(--dama-surface);
      border: 1px solid var(--dama-border);
      border-radius: var(--dama-radius-md);
      box-shadow: var(--dama-shadow-xs);
    }
    .range-value {
      color: var(--dama-text);
      font-variant-numeric: tabular-nums;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientSummary {
  private readonly paymentApi = inject(PaymentApi);
  private readonly authApi = inject(AuthApi);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  private readonly tenantId = this.authService.claims()?.tenantId ?? '';

  protected readonly timezoneOptions = TIMEZONE_OPTIONS;
  protected readonly selectedTimezone = signal(this.authService.tenantTimezone());

  readonly state = toSignal(
    this.paymentApi.getSummary().pipe(
      map((data): SummaryState => ({ kind: 'ready', data })),
      startWith<SummaryState>({ kind: 'loading' }),
      catchError(() => of<SummaryState>({ kind: 'error' })),
    ),
    { initialValue: { kind: 'loading' } as SummaryState },
  );

  readonly ready = computed<PaymentSummary | null>(() => {
    const currentState = this.state();
    return currentState.kind === 'ready' ? currentState.data : null;
  });

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
}
