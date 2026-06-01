import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { pageHeadStyles } from './page-head.variants';

@Component({
  selector: 'app-page-head',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-head.html',
  host: { class: 'block' },
})
export class PageHead {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);

  protected readonly styles = pageHeadStyles();
}
