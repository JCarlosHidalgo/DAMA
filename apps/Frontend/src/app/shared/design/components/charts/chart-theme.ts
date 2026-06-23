import { computed, inject, Signal } from '@angular/core';

import { ThemeService } from '@core/services';

import { ChartPalette, readChartPalette } from './chart-tokens.logic';

export function injectChartPalette(): Signal<ChartPalette> {
  const theme = inject(ThemeService);

  return computed(() => {
    theme.isDark();

    if (typeof document === 'undefined' || typeof getComputedStyle === 'undefined') {
      return readChartPalette(() => '');
    }

    const rootStyles = getComputedStyle(document.documentElement);
    return readChartPalette((variableName) => rootStyles.getPropertyValue(variableName));
  });
}
