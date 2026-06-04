import { describe, expect, it } from 'vitest';

import { SubscriptionPlan } from '@core/models';

import { sortPlansByLevel } from './subscription-plans';

function makePlan(level: number): SubscriptionPlan {
  return { level, price: level * 10, durationAmount: 1, durationUnit: 'Month' };
}

describe('sortPlansByLevel', () => {
  it('sorts an unsorted array by level ascending', () => {
    const plans = [makePlan(3), makePlan(1), makePlan(2)];
    expect(sortPlansByLevel(plans).map((plan) => plan.level)).toEqual([1, 2, 3]);
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
    const plans = [makePlan(2), makePlan(1)];
    sortPlansByLevel(plans);
    expect(plans[0].level).toBe(2);
  });
});
