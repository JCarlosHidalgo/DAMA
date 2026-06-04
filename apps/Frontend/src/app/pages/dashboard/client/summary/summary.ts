import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith } from 'rxjs';

import { PaymentApi } from '@core/api';
import { PaymentSummary } from '@core/models';
import { ErrorState, LoadingSkeleton, PageHead, StatCard } from '@shared/components';
import { MoneyPipe, TenantDatePipe } from '@shared/pipes';

import { clientSummaryStyles } from './summary.variants';

type SummaryState =
  | { kind: 'loading' }
  | { kind: 'ready'; data: PaymentSummary }
  | { kind: 'error' };

@Component({
  selector: 'app-client-summary',
  imports: [PageHead, StatCard, LoadingSkeleton, ErrorState, MoneyPipe, TenantDatePipe],
  template: `
    <app-page-head title="Resumen" subtitle="Ganancias y actividad reciente" />

    @switch (state().kind) {
      @case ('loading') {
        <div [class]="styles.kpiGrid()">
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
          <div [class]="styles.kpiGrid()">
            <app-stat-card
              label="Ganancias totales"
              [value]="summary.totalEarnings | money: summary.currency"
              icon="money-bill"
            />
            <app-stat-card
              label="Últimos 30 días"
              [value]="summary.monthEarnings | money: summary.currency"
              icon="credit-card"
            />
            <app-stat-card
              label="Primer pago"
              [value]="summary.firstPaymentDate | tenantDate"
              icon="calendar"
            />
            <div [class]="styles.rangeCard()">
              <span class="t-label-up">Rango consultado</span>
              <span [class]="styles.rangeValue()">
                {{ summary.from | tenantDate }} → {{ summary.to | tenantDate }}
              </span>
            </div>
          </div>
        }
      }
    }
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientSummary {
  private readonly paymentApi = inject(PaymentApi);

  protected readonly styles = clientSummaryStyles();

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
}
