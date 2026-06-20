import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ConfirmationDialog } from '@core/utils/confirmation-dialog';

describe('ConfirmationDialog', () => {
  const dialogRef = { close: vi.fn() };

  it('no tiene violaciones en modo normal', async () => {
    const { container } = await render(ConfirmationDialog, {
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        {
          provide: MAT_DIALOG_DATA,
          useValue: {
            title: 'Confirmar acción',
            message: '¿Estás seguro de continuar?',
            confirmLabel: 'Confirmar',
            cancelLabel: 'Cancelar',
            destructive: false,
          },
        },
      ],
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones en modo destructivo', async () => {
    const { container } = await render(ConfirmationDialog, {
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        {
          provide: MAT_DIALOG_DATA,
          useValue: {
            title: 'Eliminar curso',
            message: 'Esta acción no se puede deshacer.',
            confirmLabel: 'Eliminar',
            cancelLabel: 'Cancelar',
            destructive: true,
          },
        },
      ],
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
