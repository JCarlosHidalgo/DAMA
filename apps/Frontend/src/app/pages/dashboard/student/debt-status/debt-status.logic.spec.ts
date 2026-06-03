import { describe, expect, it } from 'vitest';

import { tabKindForIndex } from './debt-status.logic';

describe('tabKindForIndex', () => {
  it('returns pending for index 0', () => {
    expect(tabKindForIndex(0)).toBe('pending');
  });

  it('returns success for index 1', () => {
    expect(tabKindForIndex(1)).toBe('success');
  });

  it('returns failed for index 2', () => {
    expect(tabKindForIndex(2)).toBe('failed');
  });

  it('returns pending for an out-of-range index', () => {
    expect(tabKindForIndex(7)).toBe('pending');
  });
});
