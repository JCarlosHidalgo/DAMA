import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { loadingSkeletonStyles } from './loading-skeleton.variants';

@Component({
  selector: 'app-loading-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div
    [class]="styles.bar()"
    [style.height.px]="height()"
    [style.width]="width()"
  ></div>`,
  host: { class: 'block' },
})
export class LoadingSkeleton {
  readonly height = input<number>(16);
  readonly width = input<string>('100%');
  protected readonly styles = loadingSkeletonStyles();
}
