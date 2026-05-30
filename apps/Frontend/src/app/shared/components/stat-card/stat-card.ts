import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '../icon/icon';
import type { IconName } from '../icon/icon-registry';

export interface StatDelta {
  sign: 'up' | 'down';
  value: string;
}

@Component({
  selector: 'app-stat-card',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stat-card.html',
  styleUrl: './stat-card.scss',
})
export class StatCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly delta = input<StatDelta | null>(null);
  readonly sub = input<string | null>(null);
  readonly icon = input<IconName | null>(null);
}
