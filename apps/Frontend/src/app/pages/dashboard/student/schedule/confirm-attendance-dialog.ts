import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { CourseScheduleEntry } from '@core/models';
import { NotificationService } from '@core/services';
import { ClassKindStrategies } from '@core/strategies';
import { Icon } from '@shared/components';

export interface ConfirmAttendanceDialogData {
  entry: CourseScheduleEntry;
}

type SubmitState = 'idle' | 'submitting';

@Component({
  selector: 'app-confirm-attendance-dialog',
  imports: [MatDialogModule, MatButtonModule, MatProgressSpinnerModule, Icon],
  template: `
    <h2 mat-dialog-title>Confirmar asistencia</h2>
    <mat-dialog-content>
      <div class="detail">
        <span class="course">{{ data.entry.courseName }}</span>
        <span class="time">
          <app-icon name="clock" />
          {{ data.entry.startTime.slice(0, 5) }} – {{ data.entry.endTime.slice(0, 5) }}
        </span>
        <span class="date t-small">{{ data.entry.date }}</span>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [disabled]="state() === 'submitting'" (click)="dialogRef.close()">
        Cancelar
      </button>
      <button
        mat-flat-button
        color="primary"
        [disabled]="state() === 'submitting'"
        (click)="confirm()"
      >
        @if (state() === 'submitting') {
          <mat-spinner diameter="18" />
        } @else {
          Confirmar
        }
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .detail {
      display: flex;
      flex-direction: column;
      gap: 6px;
      min-width: 280px;
      padding: 8px 0;
    }
    .course {
      font-weight: 600;
      font-size: 1.1rem;
    }
    .time {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-variant-numeric: tabular-nums;
    }
    .date {
      color: var(--dama-text-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmAttendanceDialog {
  readonly dialogRef = inject(MatDialogRef<ConfirmAttendanceDialog, boolean>);
  readonly data = inject<ConfirmAttendanceDialogData>(MAT_DIALOG_DATA);
  private readonly notifications = inject(NotificationService);
  private readonly classKindStrategies = inject(ClassKindStrategies);

  protected readonly state = signal<SubmitState>('idle');

  async confirm(): Promise<void> {
    if (this.state() === 'submitting') {
      return;
    }
    this.state.set('submitting');
    try {
      const strategy = this.classKindStrategies.for(this.data.entry.classKind);
      await firstValueFrom(
        strategy.markAttendance({
          classId: this.data.entry.classId,
          courseName: this.data.entry.courseName,
        }),
      );
      this.notifications.success('Asistencia registrada.', { duration: 3000 });
      this.dialogRef.close(true);
    } catch (error: unknown) {
      this.notifications.error(this.translateOutcome(error));
      this.state.set('idle');
    }
  }

  private translateOutcome(error: unknown): string {
    const message = error instanceof Error ? error.message : '';
    if (message.includes('AlreadyMarked')) {
      return 'Ya registraste tu asistencia a esta clase.';
    }
    if (message.includes('NoRemainingClasses')) {
      return 'No tienes clases disponibles. Compra un paquete primero.';
    }
    if (message.includes('ClassFull')) {
      return 'La clase ya alcanzó su cupo máximo.';
    }
    if (message.includes('OutsideAllowedWindow')) {
      return 'Fuera del horario permitido (01:00–23:00 local).';
    }
    return 'No se pudo registrar la asistencia.';
  }
}
