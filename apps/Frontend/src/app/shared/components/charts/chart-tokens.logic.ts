export type CssVarReader = (variableName: string) => string;

export interface ChartPalette {
  primary: string;
  success: string;
  warning: string;
  danger: string;
  text: string;
  textMuted: string;
  grid: string;
  surface: string;
}

export type ChartPaletteKey = keyof ChartPalette;

const FALLBACK_PALETTE: ChartPalette = {
  primary: '#e0703c',
  success: '#3fae6b',
  warning: '#d99a2b',
  danger: '#c2415b',
  text: '#2a2521',
  textMuted: '#6b625a',
  grid: '#e6ddd5',
  surface: '#fffdfb',
};

export function readChartPalette(read: CssVarReader): ChartPalette {
  const resolve = (variableName: string, fallback: string): string => {
    const value = read(variableName).trim();
    return value.length > 0 ? value : fallback;
  };

  return {
    primary: resolve('--dama-primary', FALLBACK_PALETTE.primary),
    success: resolve('--dama-success', FALLBACK_PALETTE.success),
    warning: resolve('--dama-warning', FALLBACK_PALETTE.warning),
    danger: resolve('--dama-danger', FALLBACK_PALETTE.danger),
    text: resolve('--dama-text', FALLBACK_PALETTE.text),
    textMuted: resolve('--dama-text-muted', FALLBACK_PALETTE.textMuted),
    grid: resolve('--dama-divider', FALLBACK_PALETTE.grid),
    surface: resolve('--dama-surface', FALLBACK_PALETTE.surface),
  };
}

export const CHART_SERIES_ORDER: readonly ChartPaletteKey[] = [
  'primary',
  'success',
  'warning',
  'danger',
  'textMuted',
];

export function seriesColor(index: number, palette: ChartPalette): string {
  const key = CHART_SERIES_ORDER[index % CHART_SERIES_ORDER.length];
  return palette[key];
}

export function withAlpha(color: string, alphaPercent: number): string {
  return `color-mix(in oklch, ${color} ${alphaPercent}%, transparent)`;
}
