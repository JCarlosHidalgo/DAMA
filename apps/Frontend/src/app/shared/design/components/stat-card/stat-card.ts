import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Icon } from '@shared/design/components/icon';
import type { IconName } from '@shared/design/components/icon';
import { statCardStyles } from './stat-card.variants';

export interface StatDelta {
  sign: 'up' | 'down';
  value: string;
}

@Component({
  selector: 'app-stat-card',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stat-card.html',
  host: { class: 'block' },
})
export class StatCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly delta = input<StatDelta | null>(null);
  readonly sub = input<string | null>(null);
  readonly icon = input<IconName | null>(null);

  protected readonly styles = computed(() => statCardStyles({ sign: this.delta()?.sign }));
}
