import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="dama-skel" [style.height.px]="height()" [style.width]="width()"></div>`,
  styleUrl: './loading-skeleton.scss',
})
export class LoadingSkeleton {
  readonly height = input<number>(16);
  readonly width = input<string>('100%');
}
