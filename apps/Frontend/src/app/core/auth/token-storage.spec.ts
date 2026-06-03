import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { SessionStorageTokenStorage } from './token-storage';

const STORAGE_KEY = 'dama.accessToken';
const REFRESH_KEY = 'dama.refreshToken';

describe('SessionStorageTokenStorage', () => {
  let tokenStorage: SessionStorageTokenStorage;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    tokenStorage = TestBed.inject(SessionStorageTokenStorage);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    sessionStorage.clear();
  });

  describe('when sessionStorage is available', () => {
    it('returns null on read when no token has been written', () => {
      expect(tokenStorage.read()).toBeNull();
    });

    it('persists and reads back the token via sessionStorage', () => {
      tokenStorage.write('access-token-abc');

      expect(sessionStorage.getItem(STORAGE_KEY)).toBe('access-token-abc');
      expect(tokenStorage.read()).toBe('access-token-abc');
    });

    it('overwrites the previous token on subsequent writes', () => {
      tokenStorage.write('first');
      tokenStorage.write('second');

      expect(tokenStorage.read()).toBe('second');
    });

    it('returns null on readRefresh when no refresh token has been written', () => {
      expect(tokenStorage.readRefresh()).toBeNull();
    });

    it('persists and reads back the refresh token via sessionStorage', () => {
      tokenStorage.writeRefresh('refresh-token-xyz');

      expect(sessionStorage.getItem(REFRESH_KEY)).toBe('refresh-token-xyz');
      expect(tokenStorage.readRefresh()).toBe('refresh-token-xyz');
    });

    it('removes both the access and refresh entries on clear', () => {
      tokenStorage.write('to-be-cleared');
      tokenStorage.writeRefresh('refresh-to-be-cleared');
      tokenStorage.clear();

      expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull();
      expect(sessionStorage.getItem(REFRESH_KEY)).toBeNull();
      expect(tokenStorage.read()).toBeNull();
      expect(tokenStorage.readRefresh()).toBeNull();
    });
  });

  describe('when sessionStorage is unavailable (SSR-like)', () => {
    beforeEach(() => {
      vi.stubGlobal('sessionStorage', undefined);
    });

    it('returns null on read without throwing', () => {
      expect(tokenStorage.read()).toBeNull();
    });

    it('write is a no-op without throwing', () => {
      expect(() => tokenStorage.write('any')).not.toThrow();
    });

    it('clear is a no-op without throwing', () => {
      expect(() => tokenStorage.clear()).not.toThrow();
    });
  });
});
