import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

import { Icon } from '@shared/components';

import { attendanceMarkedDialogStyles } from './attendance-marked-dialog.variants';

@Component({
  selector: 'app-attendance-marked-dialog',
  imports: [MatDialogModule, MatButtonModule, Icon],
  template: `
    <mat-dialog-content>
      <div [class]="styles.marked()">
        <app-icon name="check" [class]="styles.icon()" />
        <p [class]="styles.message()">Ya marcaste asistencia para esta clase</p>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" (click)="dialogRef.close()">Entendido</button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceMarkedDialog {
  readonly dialogRef = inject(MatDialogRef<AttendanceMarkedDialog>);

  protected readonly styles = attendanceMarkedDialogStyles();
}
