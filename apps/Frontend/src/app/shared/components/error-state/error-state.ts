import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '../icon/icon';

@Component({
  selector: 'app-error-state',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './error-state.html',
  styleUrl: './error-state.scss',
})
export class ErrorState {
  readonly message = input.required<string>();
}
