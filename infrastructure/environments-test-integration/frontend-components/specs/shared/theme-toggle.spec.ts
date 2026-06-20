import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { signal } from '@angular/core';
import { MockProvider } from 'ng-mocks';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ThemeToggle } from '@shared/components/theme-toggle/theme-toggle';
import { ThemeService } from '@core/services/theme-service';

describe('ThemeToggle', () => {
  it('no tiene violaciones en modo claro', async () => {
    const { container } = await render(ThemeToggle, {
      providers: [
        provideNoopAnimations(),
        MockProvider(ThemeService, { isDark: signal(false), toggle: vi.fn() }),
      ],
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones en modo oscuro', async () => {
    const { container } = await render(ThemeToggle, {
      providers: [
        provideNoopAnimations(),
        MockProvider(ThemeService, { isDark: signal(true), toggle: vi.fn() }),
      ],
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
