import { readChartPalette, seriesColor, withAlpha, type ChartPalette } from './chart-tokens.logic';

describe('chart-tokens.logic', () => {
  describe('readChartPalette', () => {
    it('maps each dama token to its palette field', () => {
      const values: Record<string, string> = {
        '--dama-primary': '#p',
        '--dama-success': '#s',
        '--dama-warning': '#w',
        '--dama-danger': '#d',
        '--dama-text': '#t',
        '--dama-text-muted': '#tm',
        '--dama-divider': '#g',
        '--dama-surface': '#sf',
      };

      const palette = readChartPalette((name) => values[name] ?? '');

      expect(palette).toEqual({
        primary: '#p',
        success: '#s',
        warning: '#w',
        danger: '#d',
        text: '#t',
        textMuted: '#tm',
        grid: '#g',
        surface: '#sf',
      });
    });

    it('falls back when a token is empty or whitespace', () => {
      const palette = readChartPalette((name) => (name === '--dama-primary' ? '   ' : ''));

      expect(palette.primary).toBe('#e0703c');
      expect(palette.danger).toBe('#c2415b');
      expect(palette.surface).toBe('#fffdfb');
    });
  });

  describe('seriesColor', () => {
    const palette: ChartPalette = {
      primary: 'p',
      success: 's',
      warning: 'w',
      danger: 'd',
      text: 't',
      textMuted: 'tm',
      grid: 'g',
      surface: 'sf',
    };

    it('returns the color at the rotating index', () => {
      expect(seriesColor(0, palette)).toBe('p');
      expect(seriesColor(1, palette)).toBe('s');
      expect(seriesColor(4, palette)).toBe('tm');
    });

    it('wraps around past the end of the order', () => {
      expect(seriesColor(5, palette)).toBe('p');
      expect(seriesColor(6, palette)).toBe('s');
    });
  });

  describe('withAlpha', () => {
    it('wraps a color in a color-mix expression', () => {
      expect(withAlpha('#abc', 18)).toBe('color-mix(in oklch, #abc 18%, transparent)');
    });
  });
});
