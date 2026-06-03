import { describe, it, expect } from 'vitest';

import {
  AppKeyState,
  asReadyAppKey,
  shouldUpdateTimezone,
  subscriptionAllowsTodotix,
} from './configuration.logic';

describe('subscriptionAllowsTodotix', () => {
  it('returns false when subscription index is below 3', () => {
    expect(subscriptionAllowsTodotix(2)).toBe(false);
  });

  it('returns true when subscription index is 3', () => {
    expect(subscriptionAllowsTodotix(3)).toBe(true);
  });

  it('returns true when subscription index is above 3', () => {
    expect(subscriptionAllowsTodotix(4)).toBe(true);
  });
});

describe('asReadyAppKey', () => {
  it('returns null for loading state', () => {
    const state: AppKeyState = { kind: 'loading' };
    expect(asReadyAppKey(state)).toBeNull();
  });

  it('returns null for error state', () => {
    const state: AppKeyState = { kind: 'error' };
    expect(asReadyAppKey(state)).toBeNull();
  });

  it('returns the state for ready state', () => {
    const state: AppKeyState = {
      kind: 'ready',
      status: { hasCustomKey: true, maskedAppKey: '••••2724' },
    };
    expect(asReadyAppKey(state)).toBe(state);
  });
});

describe('shouldUpdateTimezone', () => {
  it('returns false when next equals current', () => {
    expect(shouldUpdateTimezone('America/La_Paz', 'America/La_Paz')).toBe(false);
  });

  it('returns true when next differs from current', () => {
    expect(shouldUpdateTimezone('America/Lima', 'America/La_Paz')).toBe(true);
  });
});
