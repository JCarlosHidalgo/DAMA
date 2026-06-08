import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { barOptions, type ValueFormatter } from './chart-options.logic';
import { ChartPaletteKey } from './chart-tokens.logic';
import { injectChartPalette } from './chart-theme';
import { chartCardStyles } from './chart-card.variants';

@Component({
  selector: 'app-bar-chart',
  imports: [BaseChartDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="styles.root()">
      @if (title()) {
        <span [class]="styles.title()">{{ title() }}</span>
      }
      <div [class]="styles.canvasWrap()">
        <canvas baseChart type="bar" [data]="data()" [options]="options()"></canvas>
      </div>
    </div>
  `,
  host: { class: 'block' },
})
export class BarChart {
  readonly title = input<string>('');
  readonly labels = input.required<string[]>();
  readonly values = input.required<number[]>();
  readonly seriesLabel = input<string>('');
  readonly colorKey = input<ChartPaletteKey>('primary');
  readonly colorKeys = input<ChartPaletteKey[] | null>(null);
  readonly valueFormatter = input<ValueFormatter | null>(null);

  private readonly palette = injectChartPalette();
  protected readonly styles = chartCardStyles();

  protected readonly data = computed<ChartData<'bar', number[], string>>(() => {
    const palette = this.palette();
    const keys = this.colorKeys();
    const backgroundColor = this.values().map((_, index) =>
      keys && keys[index] ? palette[keys[index]] : palette[this.colorKey()],
    );
    return {
      labels: this.labels(),
      datasets: [
        {
          label: this.seriesLabel(),
          data: this.values(),
          backgroundColor,
          borderRadius: 6,
        },
      ],
    };
  });

  protected readonly options = computed(() =>
    barOptions(this.palette(), this.valueFormatter() ?? undefined),
  );
}
