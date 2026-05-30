import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-page-head',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-head.html',
  styleUrl: './page-head.scss',
})
export class PageHead {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
}
