import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '@shared/design/components/icon';
import type { IconName } from '@shared/design/components/icon';
import { emptyStateStyles } from './empty-state.variants';

@Component({
  selector: 'app-empty-state',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './empty-state.html',
  host: { class: 'block' },
})
export class EmptyState {
  readonly icon = input<IconName>('ban');
  readonly message = input.required<string>();

  protected readonly styles = emptyStateStyles();
}
