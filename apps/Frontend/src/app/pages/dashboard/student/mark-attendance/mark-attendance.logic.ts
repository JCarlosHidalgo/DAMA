import { ClassKind } from '@core/strategies';
import { QrPayload } from '@core/utils';

export type ScannedQrOutcome =
  | { kind: 'invalid'; message: string }
  | { kind: 'foreign'; message: string }
  | { kind: 'ok'; payload: QrPayload };

export function resolveScannedQr(
  payload: QrPayload | null,
  expectedTenantId: string,
): ScannedQrOutcome {
  if (!payload) {
    return { kind: 'invalid', message: 'Código QR no válido.' };
  }
  if (payload.tenantId !== expectedTenantId) {
    return { kind: 'foreign', message: 'Este QR no corresponde a tu cuenta.' };
  }
  return { kind: 'ok', payload };
}

export function classKindFromPayload(payloadKind: QrPayload['kind']): ClassKind {
  return payloadKind === 'SCHEDULED' ? 'Scheduled' : 'Unique';
}

export type MarkErrorOutcome = { kind: 'alreadyMarked' } | { kind: 'fail'; message: string };

export function classifyMarkAttendanceError(error: unknown): MarkErrorOutcome {
  if (error instanceof Error && error.message.includes('AlreadyMarked')) {
    return { kind: 'alreadyMarked' };
  }
  const message =
    error instanceof Error && error.message.includes('OutsideAllowedWindow')
      ? 'Fuera del horario permitido (01:00–23:00 local).'
      : 'No se pudo registrar la asistencia.';
  return { kind: 'fail', message };
}
