import { describe, expect, it } from 'vitest';

import {
  DURATION_UNITS,
  planUpdatedMessage,
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
