import { describe, expect, it } from 'vitest';

import { scheduleSubtitle } from './schedule.logic';

describe('scheduleSubtitle', () => {
  it('returns the interactable subtitle when true', () => {
    expect(scheduleSubtitle(true)).toBe('Toca una clase para abrir el QR de asistencia');
  });

  it('returns the read-only subtitle when false', () => {
    expect(scheduleSubtitle(false)).toBe('Vista de solo lectura');
  });
});
