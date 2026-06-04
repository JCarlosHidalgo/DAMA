import { QrDebtStatus } from '@core/models';

export type QrDebtOutcome =
  | { kind: 'qr'; debtId: string; qrUrl: string }
  | { kind: 'failed'; message: string }
  | { kind: 'pending'; message: string };

export function resolveQrDebtOutcome(status: QrDebtStatus, pendingMessage: string): QrDebtOutcome {
  if (status.status === 'Ready' && status.qrSimpleUrl) {
    return { kind: 'qr', debtId: status.identificadorDeuda, qrUrl: status.qrSimpleUrl };
  }
  if (status.status === 'Failed') {
    return {
      kind: 'failed',
      message: `Error al generar QR: ${status.error ?? 'reintente más tarde.'}`,
    };
  }
  return { kind: 'pending', message: pendingMessage };
}
