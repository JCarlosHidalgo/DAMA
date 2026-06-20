import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialogRef } from '@angular/material/dialog';
import { AttendanceMarkedDialog } from '@core/utils/attendance-marked-dialog';

describe('AttendanceMarkedDialog', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(AttendanceMarkedDialog, {
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
      ],
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
