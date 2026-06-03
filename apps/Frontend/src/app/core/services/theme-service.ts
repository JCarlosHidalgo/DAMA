import { computed, effect, Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'dama.theme';

const SURFACE_COLOR: Record<Theme, string> = {
  light: '#fffdfb',
  dark: '#2a2521',
};

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly theme = signal<Theme>(this.readInitialTheme());

  readonly current = computed<Theme>(() => this.theme());
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    effect(() => {
      const theme = this.theme();
      this.applyTheme(theme);
      this.persistTheme(theme);
    });
  }

  toggle(): void {
    this.theme.update((theme) => (theme === 'dark' ? 'light' : 'dark'));
  }

  set(theme: Theme): void {
    this.theme.set(theme);
  }

  private readInitialTheme(): Theme {
    const stored = this.readStoredTheme();
    if (stored) {
      return stored;
    }
    if (
      typeof window !== 'undefined' &&
      typeof window.matchMedia === 'function' &&
      window.matchMedia('(prefers-color-scheme: dark)').matches
    ) {
      return 'dark';
    }
    return 'light';
  }

  private readStoredTheme(): Theme | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }
    const value = localStorage.getItem(THEME_STORAGE_KEY);
    return value === 'light' || value === 'dark' ? value : null;
  }

  private persistTheme(theme: Theme): void {
    if (typeof localStorage === 'undefined') {
      return;
    }
    localStorage.setItem(THEME_STORAGE_KEY, theme);
  }

  private applyTheme(theme: Theme): void {
    if (typeof document === 'undefined') {
      return;
    }
    document.documentElement.classList.toggle('dark', theme === 'dark');
    const meta = document.querySelector('meta[name="theme-color"]');
    if (meta) {
      meta.setAttribute('content', SURFACE_COLOR[theme]);
    }
  }
}
