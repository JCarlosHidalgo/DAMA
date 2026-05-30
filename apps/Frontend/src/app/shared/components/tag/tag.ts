import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type TagVariant = 'neutral' | 'primary' | 'success' | 'warning' | 'danger';

@Component({
  selector: 'app-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tag.html',
  styleUrl: './tag.scss',
})
export class Tag {
  readonly variant = input<TagVariant>('neutral');
  readonly dot = input<boolean>(false);
}
