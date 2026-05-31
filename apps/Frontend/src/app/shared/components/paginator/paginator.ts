import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { Icon } from '@shared/components/icon';

export interface PageInfo {
  currentIndex: number;
  maxIndex: number;
}

@Component({
  selector: 'app-paginator',
  imports: [MatIconButton, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './paginator.html',
  styleUrl: './paginator.scss',
})
export class Paginator {
  readonly page = input.required<PageInfo>();
  readonly pageChange = output<number>();

  prev(): void {
    const p = this.page();
    if (p.currentIndex > 0) this.pageChange.emit(p.currentIndex - 1);
  }
  next(): void {
    const p = this.page();
    if (p.currentIndex < p.maxIndex) this.pageChange.emit(p.currentIndex + 1);
  }
}
