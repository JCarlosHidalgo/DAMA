import { describe, expect, it } from 'vitest';

import { SubscriptionRevenueByTier, SubscriptionRevenuePoint, TenantTierCount } from '@core/models';

import { revenueByTierToBar, revenueTimelineToLine, tierCountsToDoughnut } from './analytics.logic';

describe('tierCountsToDoughnut', () => {
  it('labels tiers and assigns rotating color keys', () => {
    const tiers: TenantTierCount[] = [
      { tier: 1, tenantCount: 4 },
      { tier: 2, tenantCount: 2 },
      { tier: 3, tenantCount: 1 },
    ];

    expect(tierCountsToDoughnut(tiers)).toEqual({
      labels: ['Nivel 1', 'Nivel 2', 'Nivel 3'],
      values: [4, 2, 1],
      colorKeys: ['primary', 'success', 'warning'],
    });
  });

  it('returns empty series for no tiers', () => {
    expect(tierCountsToDoughnut([])).toEqual({ labels: [], values: [], colorKeys: [] });
  });
});

describe('revenueTimelineToLine', () => {
  it('maps points to padded YYYY-MM labels and amounts', () => {
    const points: SubscriptionRevenuePoint[] = [
      { year: 2026, month: 2, amount: 800, count: 4 },
      { year: 2026, month: 10, amount: 1200, count: 6 },
    ];

    expect(revenueTimelineToLine(points)).toEqual({
      labels: ['2026-02', '2026-10'],
      values: [800, 1200],
    });
  });
});

describe('revenueByTierToBar', () => {
  it('labels tiers and exposes their revenue', () => {
    const rows: SubscriptionRevenueByTier[] = [
      { level: 1, revenue: 300, count: 3 },
      { level: 2, revenue: 1200, count: 6 },
    ];

    expect(revenueByTierToBar(rows)).toEqual({
      labels: ['Nivel 1', 'Nivel 2'],
      values: [300, 1200],
    });
  });
});
