import { describe, expect, it } from 'vitest';

import { normalizeOptionalEmail } from './pay-classes.logic';

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
