import type {
  SubscriptionRevenueByTier,
  SubscriptionRevenuePoint,
  TenantTierCount,
} from '@core/models';
import type { ChartPaletteKey } from '@shared/components/charts';

export interface LabeledSeries {
  labels: string[];
  values: number[];
}

export interface ColoredSeries extends LabeledSeries {
  colorKeys: ChartPaletteKey[];
}

const TIER_COLOR_ORDER: readonly ChartPaletteKey[] = [
  'primary',
  'success',
  'warning',
  'danger',
  'textMuted',
];

export function tierCountsToDoughnut(tiers: TenantTierCount[]): ColoredSeries {
  return {
    labels: tiers.map((tier) => `Nivel ${tier.tier}`),
    values: tiers.map((tier) => tier.tenantCount),
    colorKeys: tiers.map((_, index) => TIER_COLOR_ORDER[index % TIER_COLOR_ORDER.length]),
  };
}

export function revenueTimelineToLine(points: SubscriptionRevenuePoint[]): LabeledSeries {
  return {
    labels: points.map((point) => `${point.year}-${String(point.month).padStart(2, '0')}`),
    values: points.map((point) => point.amount),
  };
}

export function revenueByTierToBar(rows: SubscriptionRevenueByTier[]): LabeledSeries {
  return {
    labels: rows.map((row) => `Nivel ${row.level}`),
    values: rows.map((row) => row.revenue),
  };
}
