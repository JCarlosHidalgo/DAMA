import { describe, it, expect } from 'vitest';

import { QrPayload } from '@core/utils';

import {
  classifyMarkAttendanceError,
  classKindFromPayload,
  resolveScannedQr,
  scheduledAttendanceKey,
} from './mark-attendance.logic';

const samplePayload: QrPayload = {
  tenantId: 't1',
  courseName: 'Yoga',
  kind: 'SCHEDULED',
  classId: 'class-1',
};

describe('resolveScannedQr', () => {
  it('returns invalid when payload is null', () => {
    expect(resolveScannedQr(null, 't1')).toEqual({
      kind: 'invalid',
      message: 'Código QR no válido.',
    });
  });

  it('returns foreign when tenantId does not match', () => {
    const foreignPayload: QrPayload = { ...samplePayload, tenantId: 'other' };
    expect(resolveScannedQr(foreignPayload, 't1')).toEqual({
      kind: 'foreign',
      message: 'Este QR no corresponde a tu cuenta.',
    });
  });

  it('returns ok with the payload when tenantId matches', () => {
    expect(resolveScannedQr(samplePayload, 't1')).toEqual({
      kind: 'ok',
      payload: samplePayload,
    });
  });
});

describe('classKindFromPayload', () => {
  it('maps SCHEDULED to Scheduled', () => {
    expect(classKindFromPayload('SCHEDULED')).toBe('Scheduled');
  });

  it('maps UNIQUE to Unique', () => {
    expect(classKindFromPayload('UNIQUE')).toBe('Unique');
  });
});

describe('scheduledAttendanceKey', () => {
  it('builds a pipe-separated key', () => {
    expect(scheduledAttendanceKey('c1', '2026-06-03')).toBe('c1|2026-06-03');
  });
});

describe('classifyMarkAttendanceError', () => {
  it('returns alreadyMarked when the error message includes AlreadyMarked', () => {
    expect(classifyMarkAttendanceError(new Error('SomePrefix AlreadyMarked suffix'))).toEqual({
      kind: 'alreadyMarked',
    });
  });

  it('returns fail with the window message when error includes OutsideAllowedWindow', () => {
    expect(classifyMarkAttendanceError(new Error('OutsideAllowedWindow'))).toEqual({
      kind: 'fail',
      message: 'Fuera del horario permitido (01:00–23:00 local).',
    });
  });

  it('returns fail with the generic message for an unrecognized Error', () => {
    expect(classifyMarkAttendanceError(new Error('other'))).toEqual({
      kind: 'fail',
      message: 'No se pudo registrar la asistencia.',
    });
  });

  it('returns fail with the generic message for a non-Error string', () => {
    expect(classifyMarkAttendanceError('x')).toEqual({
      kind: 'fail',
      message: 'No se pudo registrar la asistencia.',
    });
  });

  it('returns fail with the generic message for null', () => {
    expect(classifyMarkAttendanceError(null)).toEqual({
      kind: 'fail',
      message: 'No se pudo registrar la asistencia.',
    });
  });
});
