import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { tagStyles } from './tag.variants';

export type TagVariant = 'neutral' | 'primary' | 'success' | 'warning' | 'danger';

@Component({
  selector: 'app-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tag.html',
  host: {
    class: 'inline-flex',
  },
})
export class Tag {
  readonly variant = input<TagVariant>('neutral');
  readonly dot = input<boolean>(false);

  protected readonly styles = computed(() => tagStyles({ variant: this.variant() }));
}
