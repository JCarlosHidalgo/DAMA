import { Component } from '@angular/core';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';

import { ResponsiveTable, type ResponsiveTableColumn, TableCell } from './responsive-table';

@Component({
  imports: [ResponsiveTable, TableCell],
  template: `
    <app-responsive-table [columns]="columns" [rows]="rows">
      <ng-template appTableCell="name" let-row>
        <span class="cell-name">{{ row.name }}</span>
      </ng-template>
      <ng-template appTableCell="actions" let-row>
        <button class="cell-action">{{ row.name }}</button>
      </ng-template>
    </app-responsive-table>
  `,
})
class HostComponent {
  columns: ResponsiveTableColumn[] = [
    { key: 'name', header: 'Nombre' },
    { key: 'actions', header: 'Acciones', mobileLayout: 'block' },
  ];
  rows = [{ name: 'Salsa' }, { name: 'Tango' }];
}

describe('ResponsiveTable', () => {
  let fixture: ComponentFixture<HostComponent>;
  let handset: BehaviorSubject<BreakpointState>;

  beforeEach(() => {
    handset = new BehaviorSubject<BreakpointState>({ matches: false, breakpoints: {} });
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        {
          provide: BreakpointObserver,
          useValue: {
            isMatched: () => handset.value.matches,
            observe: () => handset.asObservable(),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('renders a mat-table on wide viewports', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('table[mat-table]')).not.toBeNull();
    const names = Array.from(element.querySelectorAll('.cell-name')).map(
      (node) => node.textContent,
    );
    expect(names).toEqual(['Salsa', 'Tango']);
  });

  it('renders stacked cards instead of a table on handset viewports', () => {
    handset.next({ matches: true, breakpoints: {} });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('table[mat-table]')).toBeNull();
    const names = Array.from(element.querySelectorAll('.cell-name')).map(
      (node) => node.textContent,
    );
    expect(names).toEqual(['Salsa', 'Tango']);
    expect(element.querySelectorAll('.cell-action').length).toBe(2);
  });
});
