import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

import { Icon } from '@shared/components';

@Component({
  selector: 'app-attendance-marked-dialog',
  imports: [MatDialogModule, MatButtonModule, Icon],
  template: `
    <mat-dialog-content>
      <div class="marked">
        <app-icon name="check" />
        <p class="t-h2">Ya marcaste asistencia para esta clase</p>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" (click)="dialogRef.close()">Entendido</button>
    </mat-dialog-actions>
  `,
  styles: `
    .marked {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 24px 16px 8px;
      text-align: center;
      min-width: 260px;
      app-icon {
        font-size: 64px;
        color: var(--dama-success);
      }
      p {
        margin: 0;
        color: var(--dama-text);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceMarkedDialog {
  readonly dialogRef = inject(MatDialogRef<AttendanceMarkedDialog>);
}
