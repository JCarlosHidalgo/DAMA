import { describe, expect, it } from 'vitest';

import { SubscriptionPlan } from '@core/models';

import {
  DURATION_UNITS,
  planUpdatedMessage,
  sortPlansByLevel,
  subscriptionPlanUpdatePayload,
  subscriptionUnitLabel,
} from './subscription-plans.logic';

describe('DURATION_UNITS', () => {
  it('contains Day, Week, Month in that order', () => {
    expect(DURATION_UNITS).toEqual(['Day', 'Week', 'Month']);
  });
});

describe('subscriptionUnitLabel', () => {
  it('returns Días for Day', () => {
    expect(subscriptionUnitLabel('Day')).toBe('Días');
  });

  it('returns Semanas for Week', () => {
    expect(subscriptionUnitLabel('Week')).toBe('Semanas');
  });

  it('returns Meses for Month', () => {
    expect(subscriptionUnitLabel('Month')).toBe('Meses');
  });
});

describe('sortPlansByLevel', () => {
  it('sorts an unsorted array by level', () => {
    const plans: SubscriptionPlan[] = [
      { level: 3, price: 30, durationAmount: 3, durationUnit: 'Month' },
      { level: 1, price: 10, durationAmount: 1, durationUnit: 'Month' },
      { level: 2, price: 20, durationAmount: 2, durationUnit: 'Month' },
    ];
    const result = sortPlansByLevel(plans);
    expect(result.map((plan) => plan.level)).toEqual([1, 2, 3]);
  });

  it('returns empty array for null', () => {
    expect(sortPlansByLevel(null)).toEqual([]);
  });

  it('returns empty array for undefined', () => {
    expect(sortPlansByLevel(undefined)).toEqual([]);
  });

  it('returns empty array for an empty array', () => {
    expect(sortPlansByLevel([])).toEqual([]);
  });

  it('does not mutate the original array', () => {
    const plans: SubscriptionPlan[] = [
      { level: 2, price: 20, durationAmount: 2, durationUnit: 'Month' },
      { level: 1, price: 10, durationAmount: 1, durationUnit: 'Month' },
    ];
    sortPlansByLevel(plans);
    expect(plans[0].level).toBe(2);
  });
});

describe('subscriptionPlanUpdatePayload', () => {
  it('coerces string-ish inputs to numbers and returns the payload shape', () => {
    const result = subscriptionPlanUpdatePayload({
      price: '15' as unknown as number,
      durationAmount: '3' as unknown as number,
      durationUnit: 'Week',
    });
    expect(result).toEqual({ price: 15, durationAmount: 3, durationUnit: 'Week' });
  });

  it('passes through numeric inputs unchanged', () => {
    const result = subscriptionPlanUpdatePayload({
      price: 50,
      durationAmount: 12,
      durationUnit: 'Month',
    });
    expect(result).toEqual({ price: 50, durationAmount: 12, durationUnit: 'Month' });
  });
});

describe('planUpdatedMessage', () => {
  it('returns the correct message for a given level', () => {
    expect(planUpdatedMessage(2)).toBe('Plan nivel 2 actualizado.');
  });
});
