import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '@shared/design/components/icon';

import { errorStateStyles } from './error-state.variants';

@Component({
  selector: 'app-error-state',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './error-state.html',
  host: { class: 'block' },
})
export class ErrorState {
  readonly message = input.required<string>();

  protected readonly styles = errorStateStyles();
}
