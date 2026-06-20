import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { signal } from '@angular/core';
import { MockProvider } from 'ng-mocks';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { BarChart } from '@shared/components/charts/bar-chart';
import { ThemeService } from '@core/services/theme-service';

describe('BarChart', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(BarChart, {
      providers: [
        provideNoopAnimations(),
        MockProvider(ThemeService, { isDark: signal(false) }),
      ],
      inputs: {
        labels: ['Ene', 'Feb', 'Mar'],
        values: [10, 25, 18],
        seriesLabel: 'Ingresos',
        title: 'Ingresos mensuales',
      },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
