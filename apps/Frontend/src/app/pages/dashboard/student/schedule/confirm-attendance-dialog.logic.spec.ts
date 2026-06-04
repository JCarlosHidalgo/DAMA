import { describe, it, expect } from 'vitest';

import { translateAttendanceError } from './confirm-attendance-dialog.logic';

describe('translateAttendanceError', () => {
  it('returns the AlreadyMarked message', () => {
    expect(translateAttendanceError(new Error('AlreadyMarked'))).toBe(
      'Ya registraste tu asistencia a esta clase.',
    );
  });

  it('returns the NoRemainingClasses message', () => {
    expect(translateAttendanceError(new Error('NoRemainingClasses'))).toBe(
      'No tienes clases disponibles. Compra un paquete primero.',
    );
  });

  it('returns the ClassFull message', () => {
    expect(translateAttendanceError(new Error('ClassFull'))).toBe(
      'La clase ya alcanzó su cupo máximo.',
    );
  });

  it('returns the OutsideAllowedWindow message', () => {
    expect(translateAttendanceError(new Error('OutsideAllowedWindow'))).toBe(
      'Fuera del horario permitido (01:00–23:00 local).',
    );
  });

  it('returns the default message for an unrecognised error', () => {
    expect(translateAttendanceError(new Error('something else'))).toBe(
      'No se pudo registrar la asistencia.',
    );
  });

  it('returns the default message for a non-Error value', () => {
    expect(translateAttendanceError(null)).toBe('No se pudo registrar la asistencia.');
  });
});
