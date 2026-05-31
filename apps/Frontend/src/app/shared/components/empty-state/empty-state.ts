import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '@shared/components/icon';
import type { IconName } from '@shared/components/icon';

@Component({
  selector: 'app-empty-state',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
})
export class EmptyState {
  readonly icon = input<IconName>('ban');
  readonly message = input.required<string>();
}
