import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-confirmation-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p class="t-body">{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close(false)">
        {{ data.cancelLabel ?? 'Cancelar' }}
      </button>
      <button
        mat-flat-button
        color="primary"
        [class.destructive]="data.destructive"
        (click)="dialogRef.close(true)"
      >
        {{ data.confirmLabel ?? 'Confirmar' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    p {
      margin: 0;
      color: var(--dama-text);
    }
    .destructive {
      --mdc-filled-button-container-color: var(--dama-danger);
      --mdc-filled-button-label-text-color: white;
    }
    .destructive:hover {
      --mdc-filled-button-container-color: color-mix(in oklab, var(--dama-danger) 85%, black);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmationDialog {
  readonly dialogRef = inject(MatDialogRef<ConfirmationDialog, boolean>);
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
