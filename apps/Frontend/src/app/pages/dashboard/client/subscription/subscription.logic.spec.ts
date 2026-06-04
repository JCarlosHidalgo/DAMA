import { describe, expect, it } from 'vitest';

import { SubscriptionPlan } from '@core/models';

import {
  describePlanDuration,
  subscriptionExpiresLabel,
  subscriptionLevelLabel,
  subscriptionPayConfirmMessage,
} from './subscription.logic';

function makePlan(overrides: Partial<SubscriptionPlan> = {}): SubscriptionPlan {
  return {
    level: 1,
    price: 100,
    durationAmount: 1,
    durationUnit: 'Month',
    ...overrides,
  };
}

describe('describePlanDuration', () => {
  it('formats a known unit (Month)', () => {
    const plan = makePlan({ durationAmount: 3, durationUnit: 'Month' });
    expect(describePlanDuration(plan)).toBe('3 mes(es)');
  });

  it('formats a known unit (Day)', () => {
    const plan = makePlan({ durationAmount: 7, durationUnit: 'Day' });
    expect(describePlanDuration(plan)).toBe('7 día(s)');
  });

  it('formats a known unit (Week)', () => {
    const plan = makePlan({ durationAmount: 2, durationUnit: 'Week' });
    expect(describePlanDuration(plan)).toBe('2 semana(s)');
  });

  it('falls back to the raw unit string for an unknown unit', () => {
    const plan = makePlan({ durationAmount: 5, durationUnit: 'Quarter' as never });
    expect(describePlanDuration(plan)).toBe('5 Quarter');
  });
});

describe('subscriptionLevelLabel', () => {
  it('returns the label for level 1', () => {
    expect(subscriptionLevelLabel(1)).toBe('Base — cursos y clases');
  });

  it('returns the label for level 2', () => {
    expect(subscriptionLevelLabel(2)).toBe('Intermedio — + estudiantes, profesores y asistencia');
  });

  it('returns the label for level 3', () => {
    expect(subscriptionLevelLabel(3)).toBe('Completo — + gestión de pagos');
  });

  it('falls back to "Nivel N" for unknown level', () => {
    expect(subscriptionLevelLabel(99)).toBe('Nivel 99');
  });
});

describe('subscriptionExpiresLabel', () => {
  it('returns "—" for epoch 0', () => {
    expect(subscriptionExpiresLabel(0)).toBe('—');
  });

  it('returns "—" for a negative epoch', () => {
    expect(subscriptionExpiresLabel(-1)).toBe('—');
  });

  it('returns a non-dash string for a positive epoch', () => {
    const result = subscriptionExpiresLabel(1700000000);
    expect(result).not.toBe('—');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });
});

describe('subscriptionPayConfirmMessage', () => {
  it('returns the confirm message for the given level', () => {
    expect(subscriptionPayConfirmMessage(2)).toBe('¿Registrar la deuda para el nivel 2?');
  });

  it('returns the confirm message for level 1', () => {
    expect(subscriptionPayConfirmMessage(1)).toBe('¿Registrar la deuda para el nivel 1?');
  });
});
