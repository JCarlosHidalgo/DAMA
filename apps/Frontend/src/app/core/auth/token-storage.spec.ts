import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { SessionStorageTokenStorage } from './token-storage';

const STORAGE_KEY = 'dama.accessToken';

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

    it('removes the entry from sessionStorage on clear', () => {
      tokenStorage.write('to-be-cleared');
      tokenStorage.clear();

      expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull();
      expect(tokenStorage.read()).toBeNull();
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
