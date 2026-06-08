import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { doughnutOptions, type ValueFormatter } from './chart-options.logic';
import { ChartPaletteKey, seriesColor } from './chart-tokens.logic';
import { injectChartPalette } from './chart-theme';
import { chartCardStyles } from './chart-card.variants';

@Component({
  selector: 'app-doughnut-chart',
  imports: [BaseChartDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="styles.root()">
      @if (title()) {
        <span [class]="styles.title()">{{ title() }}</span>
      }
      <div [class]="styles.canvasWrap()">
        <canvas baseChart type="doughnut" [data]="data()" [options]="options()"></canvas>
      </div>
    </div>
  `,
  host: { class: 'block' },
})
export class DoughnutChart {
  readonly title = input<string>('');
  readonly labels = input.required<string[]>();
  readonly values = input.required<number[]>();
  readonly colorKeys = input<ChartPaletteKey[] | null>(null);
  readonly valueFormatter = input<ValueFormatter | null>(null);

  private readonly palette = injectChartPalette();
  protected readonly styles = chartCardStyles();

  protected readonly data = computed<ChartData<'doughnut', number[], string>>(() => {
    const palette = this.palette();
    const keys = this.colorKeys();
    const backgroundColor = this.values().map((_, index) =>
      keys && keys[index] ? palette[keys[index]] : seriesColor(index, palette),
    );
    return {
      labels: this.labels(),
      datasets: [{ data: this.values(), backgroundColor, borderWidth: 0 }],
    };
  });

  protected readonly options = computed(() =>
    doughnutOptions(this.palette(), this.valueFormatter() ?? undefined),
  );
}
