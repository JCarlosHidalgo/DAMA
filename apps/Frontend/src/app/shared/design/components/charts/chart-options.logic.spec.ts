import type { TooltipItem } from 'chart.js';

import { barOptions, doughnutOptions, lineOptions } from './chart-options.logic';
import type { ChartPalette } from './chart-tokens.logic';

const palette: ChartPalette = {
  primary: 'p',
  success: 's',
  warning: 'w',
  danger: 'd',
  text: 'text-color',
  textMuted: 'muted-color',
  grid: 'grid-color',
  surface: 'sf',
};

describe('chart-options.logic', () => {
  describe('lineOptions', () => {
    it('builds responsive cartesian options themed from the palette', () => {
      const options = lineOptions(palette);

      expect(options?.responsive).toBe(true);
      expect(options?.maintainAspectRatio).toBe(false);
      expect(options?.plugins?.legend?.display).toBe(false);
      expect(options?.scales?.['y']?.ticks?.color).toBe('muted-color');
    });

    it('formats the tooltip value when a formatter is provided, else stringifies', () => {
      const formatted = lineOptions(palette, (value) => `Bs ${value}`);
      const labelFn = formatted?.plugins?.tooltip?.callbacks?.label;
      const item = { parsed: { y: 120 } } as unknown as TooltipItem<'line'>;
      expect((labelFn as (item: TooltipItem<'line'>) => string)(item)).toBe('Bs 120');

      const plain = lineOptions(palette);
      const plainLabelFn = plain?.plugins?.tooltip?.callbacks?.label;
      expect((plainLabelFn as (item: TooltipItem<'line'>) => string)(item)).toBe('120');

      const nullItem = { parsed: { y: null } } as unknown as TooltipItem<'line'>;
      expect((plainLabelFn as (item: TooltipItem<'line'>) => string)(nullItem)).toBe('0');
      const formattedNull = lineOptions(palette, (value) => `Bs ${value}`);
      const formattedNullFn = formattedNull?.plugins?.tooltip?.callbacks?.label;
      expect((formattedNullFn as (item: TooltipItem<'line'>) => string)(nullItem)).toBe('Bs 0');
    });
  });

  describe('barOptions', () => {
    it('builds cartesian options', () => {
      const options = barOptions(palette);
      expect(options?.scales?.['x']?.ticks?.color).toBe('muted-color');
      expect(options?.plugins?.legend?.display).toBe(false);
    });

    it('formats or stringifies the tooltip value', () => {
      const item = { parsed: { y: 50 } } as unknown as TooltipItem<'bar'>;

      const formatted = barOptions(palette, (value) => `#${value}`);
      const labelFn = formatted?.plugins?.tooltip?.callbacks?.label;
      expect((labelFn as (item: TooltipItem<'bar'>) => string)(item)).toBe('#50');

      const plain = barOptions(palette);
      const plainLabelFn = plain?.plugins?.tooltip?.callbacks?.label;
      expect((plainLabelFn as (item: TooltipItem<'bar'>) => string)(item)).toBe('50');

      const nullItem = { parsed: { y: null } } as unknown as TooltipItem<'bar'>;
      expect((plainLabelFn as (item: TooltipItem<'bar'>) => string)(nullItem)).toBe('0');
      const formattedNull = barOptions(palette, (value) => `#${value}`);
      const formattedNullFn = formattedNull?.plugins?.tooltip?.callbacks?.label;
      expect((formattedNullFn as (item: TooltipItem<'bar'>) => string)(nullItem)).toBe('#0');
    });
  });

  describe('doughnutOptions', () => {
    it('positions the legend at the bottom and colors its labels', () => {
      const options = doughnutOptions(palette);
      expect(options?.plugins?.legend?.position).toBe('bottom');
      expect(options?.plugins?.legend?.labels?.color).toBe('text-color');
    });

    it('formats or stringifies the tooltip value with the slice label', () => {
      const item = { label: 'Pagadas', parsed: 500 } as unknown as TooltipItem<'doughnut'>;

      const formatted = doughnutOptions(palette, (value) => `Bs ${value}`);
      const labelFn = formatted?.plugins?.tooltip?.callbacks?.label;
      expect((labelFn as (item: TooltipItem<'doughnut'>) => string)(item)).toBe('Pagadas: Bs 500');

      const plain = doughnutOptions(palette);
      const plainLabelFn = plain?.plugins?.tooltip?.callbacks?.label;
      expect((plainLabelFn as (item: TooltipItem<'doughnut'>) => string)(item)).toBe(
        'Pagadas: 500',
      );
    });
  });
});
