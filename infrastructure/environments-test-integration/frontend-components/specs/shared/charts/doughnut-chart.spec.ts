import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { signal } from '@angular/core';
import { MockProvider } from 'ng-mocks';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { DoughnutChart } from '@shared/components/charts/doughnut-chart';
import { ThemeService } from '@core/services/theme-service';

describe('DoughnutChart', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(DoughnutChart, {
      providers: [
        provideNoopAnimations(),
        MockProvider(ThemeService, { isDark: signal(false) }),
      ],
      inputs: {
        labels: ['Aprobados', 'Pendientes', 'Rechazados'],
        values: [60, 30, 10],
        title: 'Estado de pagos',
        colorKeys: null,
        valueFormatter: null,
      },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
