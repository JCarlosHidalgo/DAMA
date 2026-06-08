import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { lineOptions, type ValueFormatter } from './chart-options.logic';
import { ChartPaletteKey, withAlpha } from './chart-tokens.logic';
import { injectChartPalette } from './chart-theme';
import { chartCardStyles } from './chart-card.variants';

@Component({
  selector: 'app-line-chart',
  imports: [BaseChartDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="styles.root()">
      @if (title()) {
        <span [class]="styles.title()">{{ title() }}</span>
      }
      <div [class]="styles.canvasWrap()">
        <canvas baseChart type="line" [data]="data()" [options]="options()"></canvas>
      </div>
    </div>
  `,
  host: { class: 'block' },
})
export class LineChart {
  readonly title = input<string>('');
  readonly labels = input.required<string[]>();
  readonly values = input.required<number[]>();
  readonly seriesLabel = input<string>('');
  readonly area = input<boolean>(false);
  readonly colorKey = input<ChartPaletteKey>('primary');
  readonly valueFormatter = input<ValueFormatter | null>(null);

  private readonly palette = injectChartPalette();
  protected readonly styles = chartCardStyles();

  protected readonly data = computed<ChartData<'line', number[], string>>(() => {
    const color = this.palette()[this.colorKey()];
    return {
      labels: this.labels(),
      datasets: [
        {
          label: this.seriesLabel(),
          data: this.values(),
          borderColor: color,
          backgroundColor: this.area() ? withAlpha(color, 18) : color,
          fill: this.area(),
          tension: 0.3,
          pointRadius: 3,
        },
      ],
    };
  });

  protected readonly options = computed(() =>
    lineOptions(this.palette(), this.valueFormatter() ?? undefined),
  );
}
