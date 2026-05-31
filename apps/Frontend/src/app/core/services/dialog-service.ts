import { Injectable, Type, inject } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { ConfirmationDialog, ConfirmDialogData } from '@core/utils';

const DEFAULT_CONFIRM_WIDTH = '420px';

@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly matDialog = inject(MatDialog);

  async confirm(data: ConfirmDialogData): Promise<boolean> {
    const dialogRef = this.matDialog.open<ConfirmationDialog, ConfirmDialogData, boolean>(
      ConfirmationDialog,
      { data, width: DEFAULT_CONFIRM_WIDTH },
    );
    const result = await firstValueFrom(dialogRef.afterClosed());
    return result ?? false;
  }

  async openForm<TComponent, TInput, TResult>(
    component: Type<TComponent>,
    data: TInput,
    config?: Omit<MatDialogConfig<TInput>, 'data'>,
  ): Promise<TResult | undefined> {
    const dialogRef = this.matDialog.open<TComponent, TInput, TResult>(component, {
      data,
      ...config,
    });
    return await firstValueFrom(dialogRef.afterClosed());
  }
}
