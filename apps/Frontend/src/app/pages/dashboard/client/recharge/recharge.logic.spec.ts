import { describe, it, expect } from 'vitest';

import {
  studentRechargeConfirmMessage,
  tenantRechargeConfirmMessage,
  studentRechargeSuccessMessage,
  tenantRechargeSuccessMessage,
} from './recharge.logic';

describe('studentRechargeConfirmMessage', () => {
  it('builds the confirmation message for a student recharge', () => {
    expect(studentRechargeConfirmMessage(3, 'Ada Lovelace')).toBe(
      'Agregar 3 clase(s) a Ada Lovelace?',
    );
  });

  it('uses the exact quantity and name provided', () => {
    expect(studentRechargeConfirmMessage(10, 'Juan García')).toBe(
      'Agregar 10 clase(s) a Juan García?',
    );
  });
});

describe('tenantRechargeConfirmMessage', () => {
  it('builds the tenant-wide confirmation message', () => {
    expect(tenantRechargeConfirmMessage(5)).toBe(
      'Agregar 5 clase(s) a TODOS los estudiantes con saldo previo. ¿Continuar?',
    );
  });

  it('uses the exact quantity provided', () => {
    expect(tenantRechargeConfirmMessage(1)).toBe(
      'Agregar 1 clase(s) a TODOS los estudiantes con saldo previo. ¿Continuar?',
    );
  });
});

describe('studentRechargeSuccessMessage', () => {
  it('builds the success message for a student recharge', () => {
    expect(studentRechargeSuccessMessage(3, 'Ada Lovelace')).toBe(
      'Recargadas 3 clase(s) a Ada Lovelace.',
    );
  });

  it('uses the exact quantity and name provided', () => {
    expect(studentRechargeSuccessMessage(10, 'Juan García')).toBe(
      'Recargadas 10 clase(s) a Juan García.',
    );
  });
});

describe('tenantRechargeSuccessMessage', () => {
  it('builds the success message with the affected count', () => {
    expect(tenantRechargeSuccessMessage(7)).toBe('Actualizados 7 estudiantes.');
  });

  it('uses the exact affected count provided', () => {
    expect(tenantRechargeSuccessMessage(0)).toBe('Actualizados 0 estudiantes.');
  });
});
