import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  DestroyRef,
  Directive,
  inject,
  input,
  signal,
  TemplateRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatTableModule } from '@angular/material/table';

import { responsiveTableStyles } from './responsive-table.variants';

export interface ResponsiveTableColumn {
  key: string;
  header: string;
  mobileLayout?: 'row' | 'block';
}

@Directive({ selector: '[appTableCell]' })
export class TableCell {
  readonly appTableCell = input.required<string>();
  readonly template = inject<TemplateRef<unknown>>(TemplateRef);
}

@Component({
  selector: 'app-responsive-table',
  imports: [MatTableModule, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isHandset()) {
      <div [class]="styles.cardList()">
        @for (row of rows(); track $index) {
          <div [class]="styles.card()">
            @for (column of columns(); track column.key) {
              @if (column.mobileLayout === 'block') {
                <div [class]="styles.cardBlock()">
                  <ng-container
                    [ngTemplateOutlet]="templateFor(column.key)"
                    [ngTemplateOutletContext]="{ $implicit: row }"
                  />
                </div>
              } @else {
                <div [class]="styles.cardRow()">
                  <span [class]="styles.cardLabel()">{{ column.header }}</span>
                  <span [class]="styles.cardValue()">
                    <ng-container
                      [ngTemplateOutlet]="templateFor(column.key)"
                      [ngTemplateOutletContext]="{ $implicit: row }"
                    />
                  </span>
                </div>
              }
            }
          </div>
        }
      </div>
    } @else {
      <div [class]="styles.tableWrap()">
        <table mat-table [dataSource]="rows()" [class]="styles.table()">
          @for (column of columns(); track column.key) {
            <ng-container [matColumnDef]="column.key">
              <th mat-header-cell *matHeaderCellDef>{{ column.header }}</th>
              <td mat-cell *matCellDef="let row">
                <ng-container
                  [ngTemplateOutlet]="templateFor(column.key)"
                  [ngTemplateOutletContext]="{ $implicit: row }"
                />
              </td>
            </ng-container>
          }
          <tr mat-header-row *matHeaderRowDef="columnKeys()"></tr>
          <tr mat-row *matRowDef="let row; columns: columnKeys()"></tr>
        </table>
      </div>
    }
  `,
  host: { class: 'block' },
})
export class ResponsiveTable {
  readonly columns = input.required<ResponsiveTableColumn[]>();
  readonly rows = input.required<readonly unknown[]>();

  private readonly breakpoints = inject(BreakpointObserver);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cells = contentChildren(TableCell);

  protected readonly styles = responsiveTableStyles();
  readonly isHandset = signal(this.breakpoints.isMatched(Breakpoints.Handset));
  protected readonly columnKeys = computed(() => this.columns().map((column) => column.key));

  private readonly templateMap = computed(() => {
    const map = new Map<string, TemplateRef<unknown>>();
    for (const cell of this.cells()) {
      map.set(cell.appTableCell(), cell.template);
    }
    return map;
  });

  constructor() {
    this.breakpoints
      .observe([Breakpoints.Handset])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((state) => this.isHandset.set(state.matches));
  }

  protected templateFor(key: string): TemplateRef<unknown> | null {
    return this.templateMap().get(key) ?? null;
  }
}
