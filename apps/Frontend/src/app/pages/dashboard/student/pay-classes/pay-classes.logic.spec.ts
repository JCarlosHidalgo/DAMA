import { describe, it, expect } from 'vitest';

import { normalizeOptionalEmail, resolveQrPaymentOutcome } from './pay-classes.logic';

describe('normalizeOptionalEmail', () => {
  it('returns null for an empty string', () => {
    expect(normalizeOptionalEmail('')).toBeNull();
  });

  it('returns null for a whitespace-only string', () => {
    expect(normalizeOptionalEmail('  ')).toBeNull();
  });

  it('trims and returns the email when non-empty', () => {
    expect(normalizeOptionalEmail(' a@b.com ')).toBe('a@b.com');
  });
});

describe('resolveQrPaymentOutcome', () => {
  it('returns qr outcome when status is Ready and qrSimpleUrl is present', () => {
    expect(
      resolveQrPaymentOutcome({
        identificadorDeuda: 'debt-1',
        status: 'Ready',
        qrSimpleUrl: 'http://qr/1',
      }),
    ).toEqual({ kind: 'qr', debtId: 'debt-1', qrUrl: 'http://qr/1' });
  });

  it('returns pending outcome when status is Ready but qrSimpleUrl is missing', () => {
    expect(
      resolveQrPaymentOutcome({
        identificadorDeuda: 'debt-1',
        status: 'Ready',
      }),
    ).toEqual({
      kind: 'pending',
      message: 'Generación en curso. Revise sus pendientes en unos segundos.',
    });
  });

  it('returns failed outcome with error message when status is Failed and error is provided', () => {
    expect(
      resolveQrPaymentOutcome({
        identificadorDeuda: 'debt-1',
        status: 'Failed',
        error: 'rechazado',
      }),
    ).toEqual({ kind: 'failed', message: 'Error al generar QR: rechazado' });
  });

  it('returns failed outcome with default message when status is Failed and error is absent', () => {
    expect(
      resolveQrPaymentOutcome({
        identificadorDeuda: 'debt-1',
        status: 'Failed',
      }),
    ).toEqual({ kind: 'failed', message: 'Error al generar QR: reintente más tarde.' });
  });

  it('returns pending outcome for any other status', () => {
    expect(
      resolveQrPaymentOutcome({
        identificadorDeuda: 'debt-1',
        status: 'Pending',
      }),
    ).toEqual({
      kind: 'pending',
      message: 'Generación en curso. Revise sus pendientes en unos segundos.',
    });
  });
});
