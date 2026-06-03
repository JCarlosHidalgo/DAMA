import { describe, expect, it } from 'vitest';

import { QrDebtStatus, SubscriptionPlan } from '@core/models';

import {
  describePlanDuration,
  resolveSubscriptionQrOutcome,
  sortPlansByLevel,
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

function makeQrDebtStatus(overrides: Partial<QrDebtStatus> = {}): QrDebtStatus {
  return {
    identificadorDeuda: 'debt-123',
    status: 'Pending',
    qrSimpleUrl: null,
    error: null,
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

describe('sortPlansByLevel', () => {
  it('sorts an unsorted list by level ascending', () => {
    const plans = [makePlan({ level: 3 }), makePlan({ level: 1 }), makePlan({ level: 2 })];
    const sorted = sortPlansByLevel(plans);
    expect(sorted.map((plan) => plan.level)).toEqual([1, 2, 3]);
  });

  it('returns an empty array for null', () => {
    expect(sortPlansByLevel(null)).toEqual([]);
  });

  it('returns an empty array for undefined', () => {
    expect(sortPlansByLevel(undefined)).toEqual([]);
  });

  it('returns an empty array for an empty list', () => {
    expect(sortPlansByLevel([])).toEqual([]);
  });

  it('does not mutate the original array', () => {
    const plans = [makePlan({ level: 3 }), makePlan({ level: 1 })];
    const original = [...plans];
    sortPlansByLevel(plans);
    expect(plans.map((plan) => plan.level)).toEqual(original.map((plan) => plan.level));
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

describe('resolveSubscriptionQrOutcome', () => {
  it('returns qr outcome when status is Ready and qrSimpleUrl is present', () => {
    const status = makeQrDebtStatus({
      status: 'Ready',
      identificadorDeuda: 'debt-abc',
      qrSimpleUrl: 'https://qr.example.com/abc.png',
    });
    const outcome = resolveSubscriptionQrOutcome(status);
    expect(outcome).toEqual({
      kind: 'qr',
      debtId: 'debt-abc',
      qrUrl: 'https://qr.example.com/abc.png',
    });
  });

  it('returns pending outcome when status is Ready but qrSimpleUrl is absent', () => {
    const status = makeQrDebtStatus({ status: 'Ready', qrSimpleUrl: null });
    const outcome = resolveSubscriptionQrOutcome(status);
    expect(outcome.kind).toBe('pending');
  });

  it('returns failed outcome when status is Failed with an error message', () => {
    const status = makeQrDebtStatus({ status: 'Failed', error: 'timeout' });
    const outcome = resolveSubscriptionQrOutcome(status);
    expect(outcome.kind).toBe('failed');
    if (outcome.kind === 'failed') {
      expect(outcome.message).toContain('timeout');
    }
  });

  it('returns failed outcome with the default message when status is Failed without error', () => {
    const status = makeQrDebtStatus({ status: 'Failed', error: null });
    const outcome = resolveSubscriptionQrOutcome(status);
    expect(outcome.kind).toBe('failed');
    if (outcome.kind === 'failed') {
      expect(outcome.message).toContain('reintente más tarde.');
    }
  });

  it('returns pending outcome for any other status (e.g. Pending)', () => {
    const status = makeQrDebtStatus({ status: 'Pending' });
    const outcome = resolveSubscriptionQrOutcome(status);
    expect(outcome.kind).toBe('pending');
    if (outcome.kind === 'pending') {
      expect(outcome.message).toBe('Generación en curso. Vuelve a intentar en unos segundos.');
    }
  });
});
