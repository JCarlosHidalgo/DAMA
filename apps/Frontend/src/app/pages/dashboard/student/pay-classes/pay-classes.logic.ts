import { QrDebtStatus } from '@core/models';

export function normalizeOptionalEmail(email: string): string | null {
  return email.trim() === '' ? null : email.trim();
}

export type QrPaymentOutcome =
  | { kind: 'qr'; debtId: string; qrUrl: string }
  | { kind: 'failed'; message: string }
  | { kind: 'pending'; message: string };

export function resolveQrPaymentOutcome(status: QrDebtStatus): QrPaymentOutcome {
  if (status.status === 'Ready' && status.qrSimpleUrl) {
    return { kind: 'qr', debtId: status.identificadorDeuda, qrUrl: status.qrSimpleUrl };
  }
  if (status.status === 'Failed') {
    return {
      kind: 'failed',
      message: `Error al generar QR: ${status.error ?? 'reintente más tarde.'}`,
    };
  }
  return {
    kind: 'pending',
    message: 'Generación en curso. Revise sus pendientes en unos segundos.',
  };
}
