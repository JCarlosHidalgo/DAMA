import { TokenStorage } from '@core/auth';

export class InMemoryTokenStorage implements TokenStorage {
  private storedToken: string | null;

  constructor(initialToken: string | null = null) {
    this.storedToken = initialToken;
  }

  read(): string | null {
    return this.storedToken;
  }

  write(token: string): void {
    this.storedToken = token;
  }

  clear(): void {
    this.storedToken = null;
  }
}
