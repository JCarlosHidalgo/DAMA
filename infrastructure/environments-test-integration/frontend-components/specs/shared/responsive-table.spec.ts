import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
  ResponsiveTable,
  TableCell,
  type ResponsiveTableColumn,
} from '@shared/components/responsive-table/responsive-table';

describe('ResponsiveTable', () => {
  const columns: ResponsiveTableColumn[] = [{ key: 'name', header: 'Nombre' }];

  it('no tiene violaciones con tabla vacía', async () => {
    const { container } = await render(ResponsiveTable, {
      imports: [TableCell],
      providers: [provideNoopAnimations()],
      inputs: { columns, rows: [] },
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones con filas y template cell', async () => {
    const rows = [{ name: 'Álgebra' }, { name: 'Geometría' }];
    const { container } = await render(
      `<app-responsive-table [columns]="columns" [rows]="rows">
         <ng-template appTableCell="name" let-row>{{ row.name }}</ng-template>
       </app-responsive-table>`,
      {
        imports: [ResponsiveTable, TableCell],
        providers: [provideNoopAnimations()],
        componentProperties: { columns, rows },
      },
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
