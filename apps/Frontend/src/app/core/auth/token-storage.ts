import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'dama.accessToken';
const REFRESH_TOKEN_KEY = 'dama.refreshToken';

export interface TokenStorage {
  read(): string | null;
  write(token: string): void;
  readRefresh(): string | null;
  writeRefresh(token: string): void;
  clear(): void;
}

@Injectable({ providedIn: 'root' })
export class SessionStorageTokenStorage implements TokenStorage {
  read(): string | null {
    return this.readKey(ACCESS_TOKEN_KEY);
  }

  write(token: string): void {
    this.writeKey(ACCESS_TOKEN_KEY, token);
  }

  readRefresh(): string | null {
    return this.readKey(REFRESH_TOKEN_KEY);
  }

  writeRefresh(token: string): void {
    this.writeKey(REFRESH_TOKEN_KEY, token);
  }

  clear(): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  private readKey(key: string): string | null {
    if (typeof sessionStorage === 'undefined') {
      return null;
    }
    return sessionStorage.getItem(key);
  }

  private writeKey(key: string, value: string): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }
    sessionStorage.setItem(key, value);
  }
}
