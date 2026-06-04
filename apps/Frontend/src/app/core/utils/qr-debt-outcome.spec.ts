import { describe, expect, it } from 'vitest';

import { QrDebtStatus } from '@core/models';

import { resolveQrDebtOutcome } from './qr-debt-outcome';

const PENDING_MSG = 'Procesando su solicitud.';

function makeStatus(overrides: Partial<QrDebtStatus> = {}): QrDebtStatus {
  return {
    identificadorDeuda: 'debt-1',
    status: 'Pending',
    qrSimpleUrl: null,
    error: null,
    ...overrides,
  };
}

describe('resolveQrDebtOutcome', () => {
  it('returns qr outcome when status is Ready and qrSimpleUrl is present', () => {
    const status = makeStatus({
      status: 'Ready',
      identificadorDeuda: 'debt-abc',
      qrSimpleUrl: 'https://qr.example.com/abc.png',
    });
    expect(resolveQrDebtOutcome(status, PENDING_MSG)).toEqual({
      kind: 'qr',
      debtId: 'debt-abc',
      qrUrl: 'https://qr.example.com/abc.png',
    });
  });

  it('returns pending outcome when status is Ready but qrSimpleUrl is absent', () => {
    const status = makeStatus({ status: 'Ready', qrSimpleUrl: null });
    const outcome = resolveQrDebtOutcome(status, PENDING_MSG);
    expect(outcome).toEqual({ kind: 'pending', message: PENDING_MSG });
  });

  it('returns failed outcome with error in message when status is Failed with error', () => {
    const status = makeStatus({ status: 'Failed', error: 'timeout' });
    expect(resolveQrDebtOutcome(status, PENDING_MSG)).toEqual({
      kind: 'failed',
      message: 'Error al generar QR: timeout',
    });
  });

  it('returns failed outcome with default message when status is Failed without error', () => {
    const status = makeStatus({ status: 'Failed', error: null });
    expect(resolveQrDebtOutcome(status, PENDING_MSG)).toEqual({
      kind: 'failed',
      message: 'Error al generar QR: reintente más tarde.',
    });
  });

  it('returns pending outcome for any other status (e.g. Pending)', () => {
    const status = makeStatus({ status: 'Pending' });
    expect(resolveQrDebtOutcome(status, PENDING_MSG)).toEqual({
      kind: 'pending',
      message: PENDING_MSG,
    });
  });
});
