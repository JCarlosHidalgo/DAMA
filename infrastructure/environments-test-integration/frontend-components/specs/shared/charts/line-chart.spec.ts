import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { signal } from '@angular/core';
import { MockProvider } from 'ng-mocks';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { LineChart } from '@shared/components/charts/line-chart';
import { ThemeService } from '@core/services/theme-service';

describe('LineChart', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(LineChart, {
      providers: [
        provideNoopAnimations(),
        MockProvider(ThemeService, { isDark: signal(false) }),
      ],
      inputs: {
        labels: ['Lun', 'Mar', 'Mié', 'Jue', 'Vie'],
        values: [5, 8, 3, 10, 7],
        seriesLabel: 'Asistencias',
        title: 'Asistencia semanal',
        area: false,
        colorKey: 'primary' as const,
        valueFormatter: null,
      },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
