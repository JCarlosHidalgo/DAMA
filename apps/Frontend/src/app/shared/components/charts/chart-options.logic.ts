import type { ChartConfiguration, TooltipItem } from 'chart.js';

import { withAlpha, type ChartPalette } from './chart-tokens.logic';

export type ValueFormatter = (value: number) => string;

export function lineOptions(
  palette: ChartPalette,
  formatValue?: ValueFormatter,
): ChartConfiguration<'line'>['options'] {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (item: TooltipItem<'line'>) =>
            formatValue ? formatValue(item.parsed.y ?? 0) : String(item.parsed.y ?? 0),
        },
      },
    },
    scales: {
      x: { grid: { color: withAlpha(palette.grid, 50) }, ticks: { color: palette.textMuted } },
      y: {
        beginAtZero: true,
        grid: { color: withAlpha(palette.grid, 50) },
        ticks: { color: palette.textMuted },
      },
    },
  };
}

export function barOptions(
  palette: ChartPalette,
  formatValue?: ValueFormatter,
): ChartConfiguration<'bar'>['options'] {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (item: TooltipItem<'bar'>) =>
            formatValue ? formatValue(item.parsed.y ?? 0) : String(item.parsed.y ?? 0),
        },
      },
    },
    scales: {
      x: { grid: { color: withAlpha(palette.grid, 50) }, ticks: { color: palette.textMuted } },
      y: {
        beginAtZero: true,
        grid: { color: withAlpha(palette.grid, 50) },
        ticks: { color: palette.textMuted },
      },
    },
  };
}

export function doughnutOptions(
  palette: ChartPalette,
  formatValue?: ValueFormatter,
): ChartConfiguration<'doughnut'>['options'] {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom', labels: { color: palette.text } },
      tooltip: {
        callbacks: {
          label: (item: TooltipItem<'doughnut'>) =>
            formatValue
              ? `${item.label}: ${formatValue(item.parsed)}`
              : `${item.label}: ${item.parsed}`,
        },
      },
    },
  };
}
