import { Injectable } from '@angular/core';

const STORAGE_KEY = 'dama.accessToken';

export interface TokenStorage {
  read(): string | null;
  write(token: string): void;
  clear(): void;
}

@Injectable({ providedIn: 'root' })
export class SessionStorageTokenStorage implements TokenStorage {
  read(): string | null {
    if (typeof sessionStorage === 'undefined') {
      return null;
    }
    return sessionStorage.getItem(STORAGE_KEY);
  }

  write(token: string): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }
    sessionStorage.setItem(STORAGE_KEY, token);
  }

  clear(): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }
    sessionStorage.removeItem(STORAGE_KEY);
  }
}
