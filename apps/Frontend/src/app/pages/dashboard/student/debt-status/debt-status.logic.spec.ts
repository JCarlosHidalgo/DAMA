import { describe, expect, it } from 'vitest';

import { StudentQrBreakdown } from '@core/models';

import {
  breakdownToAmountsBar,
  breakdownToStatusDoughnut,
  failureReasonLabel,
  failureReasonTone,
  tabKindForIndex,
} from './debt-status.logic';

const breakdown: StudentQrBreakdown = {
  pendingCount: 2,
  pendingAmount: 200,
  successCount: 5,
  successAmount: 500,
  expiredCount: 1,
  expiredAmount: 100,
  otherFailedCount: 3,
  otherFailedAmount: 300,
  currency: 'BOB',
};

describe('tabKindForIndex', () => {
  it('returns pending for index 0', () => {
    expect(tabKindForIndex(0)).toBe('pending');
  });

  it('returns success for index 1', () => {
    expect(tabKindForIndex(1)).toBe('success');
  });

  it('returns failed for index 2', () => {
    expect(tabKindForIndex(2)).toBe('failed');
  });

  it('returns pending for an out-of-range index', () => {
    expect(tabKindForIndex(7)).toBe('pending');
  });
});

describe('breakdownToStatusDoughnut', () => {
  it('maps the four status counts with their color keys', () => {
    expect(breakdownToStatusDoughnut(breakdown)).toEqual({
      labels: ['Pendientes', 'Pagadas', 'Vencidas', 'Otras fallidas'],
      values: [2, 5, 1, 3],
      colorKeys: ['primary', 'success', 'warning', 'danger'],
    });
  });
});

describe('breakdownToAmountsBar', () => {
  it('maps the four status amounts with their color keys', () => {
    expect(breakdownToAmountsBar(breakdown)).toEqual({
      labels: ['Pendiente', 'Pagado', 'Vencido', 'Otras fallidas'],
      values: [200, 500, 100, 300],
      colorKeys: ['primary', 'success', 'warning', 'danger'],
    });
  });
});

describe('failureReasonTone', () => {
  it('maps each failure reason to a tag tone', () => {
    expect(failureReasonTone('Expired')).toBe('warning');
    expect(failureReasonTone('CallbackError')).toBe('danger');
    expect(failureReasonTone('Manual')).toBe('neutral');
  });
});

describe('failureReasonLabel', () => {
  it('maps each failure reason to a spanish label', () => {
    expect(failureReasonLabel('Expired')).toBe('Vencida');
    expect(failureReasonLabel('CallbackError')).toBe('Fallida');
    expect(failureReasonLabel('Manual')).toBe('Manual');
  });
});
