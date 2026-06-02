import { TokenStorage } from '@core/auth';

export class InMemoryTokenStorage implements TokenStorage {
  private storedToken: string | null;
  private storedRefreshToken: string | null;

  constructor(initialToken: string | null = null, initialRefreshToken: string | null = null) {
    this.storedToken = initialToken;
    this.storedRefreshToken = initialRefreshToken;
  }

  read(): string | null {
    return this.storedToken;
  }

  write(token: string): void {
    this.storedToken = token;
  }

  readRefresh(): string | null {
    return this.storedRefreshToken;
  }

  writeRefresh(token: string): void {
    this.storedRefreshToken = token;
  }

  clear(): void {
    this.storedToken = null;
    this.storedRefreshToken = null;
  }
}
