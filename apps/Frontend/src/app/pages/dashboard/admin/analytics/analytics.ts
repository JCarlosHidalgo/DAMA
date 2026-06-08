import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';

import { AuthApi, PaymentApi } from '@core/api';
import {
  SubscriptionRevenueByTier,
  SubscriptionRevenuePoint,
  SubscriptionRevenueTotal,
  Tenant,
  TenantTierCount,
} from '@core/models';
import { PageHead, StatCard } from '@shared/components';
import { BarChart, DoughnutChart, LineChart } from '@shared/components/charts';
import { MoneyPipe } from '@shared/pipes';

import { revenueByTierToBar, revenueTimelineToLine, tierCountsToDoughnut } from './analytics.logic';
import { adminAnalyticsStyles } from './analytics.variants';

@Component({
  selector: 'app-admin-analytics',
  imports: [PageHead, StatCard, DoughnutChart, LineChart, BarChart],
  template: `
    <app-page-head title="Análisis" subtitle="Métricas de la plataforma." />

    <div [class]="styles.kpiGrid()">
      <app-stat-card label="Tenants" [value]="tenantCount().toString()" icon="building" />
      <app-stat-card
        label="Ingresos por suscripción"
        [value]="totalRevenueLabel()"
        icon="money-bill"
      />
    </div>

    <div [class]="styles.chartsGrid()">
      @if (tierDoughnut().values.length) {
        <app-doughnut-chart
          title="Tenants por nivel"
          [labels]="tierDoughnut().labels"
          [values]="tierDoughnut().values"
          [colorKeys]="tierDoughnut().colorKeys"
        />
      }
      @if (revenueBar().values.length) {
        <app-bar-chart
          title="Ingresos por nivel"
          seriesLabel="Ingresos"
          [labels]="revenueBar().labels"
          [values]="revenueBar().values"
          colorKey="primary"
          [valueFormatter]="moneyFormatter()"
        />
      }
      @if (revenueLine().values.length) {
        <app-line-chart
          [class]="styles.timelineWide()"
          title="Ingresos por mes"
          seriesLabel="Ingresos"
          [labels]="revenueLine().labels"
          [values]="revenueLine().values"
          [area]="true"
          colorKey="success"
          [valueFormatter]="moneyFormatter()"
        />
      }
    </div>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAnalytics {
  private readonly authApi = inject(AuthApi);
  private readonly paymentApi = inject(PaymentApi);

  protected readonly styles = adminAnalyticsStyles();

  private readonly tenants = toSignal(
    this.authApi.listTenants().pipe(catchError(() => of<Tenant[]>([]))),
    { initialValue: [] as Tenant[] },
  );

  private readonly tierDistribution = toSignal(
    this.authApi.getTenantTierDistribution().pipe(catchError(() => of<TenantTierCount[]>([]))),
    { initialValue: [] as TenantTierCount[] },
  );

  private readonly revenueTotal = toSignal(
    this.paymentApi
      .getAdminRevenueTotal()
      .pipe(catchError(() => of<SubscriptionRevenueTotal | null>(null))),
    { initialValue: null as SubscriptionRevenueTotal | null },
  );

  private readonly revenueTimeline = toSignal(
    this.paymentApi
      .getAdminRevenueTimeline()
      .pipe(catchError(() => of<SubscriptionRevenuePoint[]>([]))),
    { initialValue: [] as SubscriptionRevenuePoint[] },
  );

  private readonly revenueTierRows = toSignal(
    this.paymentApi
      .getAdminRevenueByTier()
      .pipe(catchError(() => of<SubscriptionRevenueByTier[]>([]))),
    { initialValue: [] as SubscriptionRevenueByTier[] },
  );

  protected readonly tenantCount = computed(() => this.tenants().length);
  protected readonly tierDoughnut = computed(() => tierCountsToDoughnut(this.tierDistribution()));
  protected readonly revenueLine = computed(() => revenueTimelineToLine(this.revenueTimeline()));
  protected readonly revenueBar = computed(() => revenueByTierToBar(this.revenueTierRows()));

  protected readonly totalRevenueLabel = computed(() => {
    const total = this.revenueTotal();
    return total ? new MoneyPipe().transform(total.totalRevenue, total.currency) : '—';
  });

  protected readonly moneyFormatter = computed(() => {
    const currency = this.revenueTotal()?.currency ?? 'BOB';
    const pipe = new MoneyPipe();
    return (value: number): string => pipe.transform(value, currency);
  });
}
