import { describe, expect, it } from 'vitest';

import { formatTimeRange, tabKindForIndex } from './attendance-history.logic';

describe('tabKindForIndex', () => {
  it('returns scheduled for index 0', () => {
    expect(tabKindForIndex(0)).toBe('scheduled');
  });

  it('returns unique for index 1', () => {
    expect(tabKindForIndex(1)).toBe('unique');
  });

  it('returns unique for any other index', () => {
    expect(tabKindForIndex(5)).toBe('unique');
  });
});

describe('formatTimeRange', () => {
  it('slices to HH:MM and joins with en-dash', () => {
    expect(formatTimeRange('09:00:00', '10:30:00')).toBe('09:00 – 10:30');
  });
});
