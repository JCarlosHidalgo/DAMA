import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { nowInTenant, todayDateOnlyInTenant } from './tenant-time';

describe('tenant-time helpers', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-03-15T06:30:45.000Z'));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('nowInTenant', () => {
    it('returns a Date whose components reflect the tenant timezone (La Paz is UTC-4)', () => {
      const localNow = nowInTenant('America/La_Paz');
      expect(localNow.getFullYear()).toBe(2026);
      expect(localNow.getMonth()).toBe(2);
      expect(localNow.getDate()).toBe(15);
      expect(localNow.getHours()).toBe(2);
      expect(localNow.getMinutes()).toBe(30);
      expect(localNow.getSeconds()).toBe(45);
    });

    it('handles a UTC-equivalent timezone identity', () => {
      const utcNow = nowInTenant('UTC');
      expect(utcNow.getUTCDate()).toBe(15);
      expect(utcNow.getHours()).toBe(6);
      expect(utcNow.getMinutes()).toBe(30);
    });
  });

  describe('todayDateOnlyInTenant', () => {
    it('returns the local-date in yyyy-MM-dd zero-padded format', () => {
      expect(todayDateOnlyInTenant('America/La_Paz')).toBe('2026-03-15');
    });

    it('rolls over to the previous date when the local clock has not yet reached midnight UTC offset', () => {
      vi.setSystemTime(new Date('2026-03-15T02:00:00.000Z'));
      expect(todayDateOnlyInTenant('America/La_Paz')).toBe('2026-03-14');
    });
  });

  describe('fallback when Intl returns an incomplete parts list', () => {
    it('substitutes "00" for any missing field, producing a valid Date', () => {
      const originalDateTimeFormat = Intl.DateTimeFormat;
      const stubFormatter = {
        formatToParts: () => [
          { type: 'year' as Intl.DateTimeFormatPartTypes, value: '2026' },
          { type: 'month' as Intl.DateTimeFormatPartTypes, value: '03' },
          { type: 'day' as Intl.DateTimeFormatPartTypes, value: '15' },
        ],
      };
      const stubConstructor = function () {
        return stubFormatter;
      } as unknown as typeof Intl.DateTimeFormat;
      (Intl as { DateTimeFormat: typeof Intl.DateTimeFormat }).DateTimeFormat = stubConstructor;
      try {
        const fallbackNow = nowInTenant('UTC');
        expect(fallbackNow).toBeInstanceOf(Date);
        expect(Number.isNaN(fallbackNow.getTime())).toBe(false);
      } finally {
        (Intl as { DateTimeFormat: typeof Intl.DateTimeFormat }).DateTimeFormat =
          originalDateTimeFormat;
      }
    });
  });
});
