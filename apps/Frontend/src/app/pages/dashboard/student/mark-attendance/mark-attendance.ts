import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { AttendanceApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationService } from '@core/services';
import { ClassKindStrategies } from '@core/strategies';
import {
  AttendanceMarkedDialog,
  decodeQr,
  scheduledAttendanceKey,
  todayDateOnlyInTenant,
} from '@core/utils';
import { Icon, LoadingSkeleton, PageHead } from '@shared/components';
import { CameraScanner } from '@shared/components/camera-scanner/camera-scanner';

import {
  classifyMarkAttendanceError,
  classKindFromPayload,
  resolveScannedQr,
} from './mark-attendance.logic';
import { markAttendanceStatusStyles, markAttendanceStyles } from './mark-attendance.variants';

type ScanState = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-mark-attendance',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    Icon,
    PageHead,
    CameraScanner,
    LoadingSkeleton,
  ],
  template: `
    <app-page-head title="Marcar asistencia" subtitle="Escanea el código QR del profesor." />

    <mat-card [class]="styles.scannerCard()">
      <mat-card-content>
        @if (remain() !== null && remain() === 0) {
          <div [class]="styles.zeroState()">
            <app-icon name="ban" [class]="styles.zeroIcon()" />
            <p [class]="styles.zeroTitle()">No tienes clases disponibles.</p>
            <p [class]="styles.zeroHint()">Compra un paquete antes de marcar asistencia.</p>
          </div>
        } @else {
          @defer {
            <app-camera-scanner [enabled]="scannerEnabled()" (scanned)="onScan($event)" />
          } @placeholder {
            <app-loading-skeleton [height]="320" />
          }

          @switch (state()) {
            @case ('submitting') {
              <div [class]="statusStyles({ tone: 'neutral' })">
                <mat-spinner diameter="24" />
                <span class="t-body">Registrando...</span>
              </div>
            }
            @case ('success') {
              <div [class]="statusStyles({ tone: 'ok' })">
                <app-icon name="check" />
                <span class="t-body">Asistencia registrada.</span>
              </div>
            }
            @case ('error') {
              <div [class]="statusStyles({ tone: 'err' })">
                <app-icon name="warning" />
                <span class="t-body">{{ errorMessage() }}</span>
                <button mat-stroked-button (click)="reset()">Reintentar</button>
              </div>
            }
          }
        }
      </mat-card-content>
    </mat-card>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarkAttendance {
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly classKindStrategies = inject(ClassKindStrategies);
  private readonly matDialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly styles = markAttendanceStyles();
  protected readonly statusStyles = markAttendanceStatusStyles;
  protected readonly state = signal<ScanState>('idle');
  protected readonly errorMessage = signal<string>('');
  protected readonly remain = signal<number | null>(null);

  protected readonly scannerEnabled = signal(true);
  private readonly markedScheduledKeys = signal<Set<string>>(new Set());
  private readonly markedUniqueIds = signal<Set<string>>(new Set());

  constructor() {
    this.loadRemain();
    this.loadMarkedAttendance();
  }

  private async loadRemain(): Promise<void> {
    try {
      const remainResponse = await firstValueFrom(this.attendanceApi.getMyRemain());
      this.remain.set(remainResponse.numberOfClasses);
    } catch {
      this.remain.set(null);
    }
  }

  private async loadMarkedAttendance(): Promise<void> {
    const studentId = this.authService.claims()?.userId;
    if (!studentId) {
      return;
    }
    try {
      const [scheduled, unique] = await Promise.all([
        firstValueFrom(this.attendanceApi.myScheduledHistory(studentId)),
        firstValueFrom(this.attendanceApi.myUniqueHistory(studentId)),
      ]);
      this.markedScheduledKeys.set(
        new Set(
          scheduled.map((attendance) =>
            scheduledAttendanceKey(attendance.classId, attendance.classDate),
          ),
        ),
      );
      this.markedUniqueIds.set(new Set(unique.map((attendance) => attendance.classId)));
    } catch {
      this.markedScheduledKeys.set(new Set());
      this.markedUniqueIds.set(new Set());
    }
  }

  async onScan(rawQrText: string): Promise<void> {
    if (this.state() !== 'idle') {
      return;
    }
    this.scannerEnabled.set(false);

    const expectedTenantId = this.authService.claims()?.tenantId ?? '';
    const outcome = resolveScannedQr(decodeQr(rawQrText), expectedTenantId);
    if (outcome.kind === 'invalid' || outcome.kind === 'foreign') {
      this.fail(outcome.message);
      return;
    }
    const payload = outcome.payload;

    const kind = classKindFromPayload(payload.kind);
    if (this.isAlreadyMarked(kind, payload.classId)) {
      this.showAlreadyMarked();
      return;
    }

    this.state.set('submitting');
    try {
      const strategy = this.classKindStrategies.for(kind);
      await firstValueFrom(
        strategy.markAttendance({
          classId: payload.classId,
          courseName: payload.courseName,
        }),
      );
      this.state.set('success');
      this.notifications.success('Asistencia registrada.', { duration: 3000 });
      setTimeout(() => this.router.navigateByUrl('/yo/resumen'), 1200);
    } catch (error: unknown) {
      const errorOutcome = classifyMarkAttendanceError(error);
      if (errorOutcome.kind === 'alreadyMarked') {
        this.showAlreadyMarked();
        return;
      }
      this.fail(errorOutcome.message);
    }
  }

  private isAlreadyMarked(kind: 'Scheduled' | 'Unique', classId: string): boolean {
    if (kind === 'Scheduled') {
      const today = todayDateOnlyInTenant(this.authService.tenantTimezone());
      return this.markedScheduledKeys().has(scheduledAttendanceKey(classId, today));
    }
    return this.markedUniqueIds().has(classId);
  }

  private showAlreadyMarked(): void {
    this.matDialog
      .open(AttendanceMarkedDialog, { width: '380px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe(() => this.reset());
  }

  private fail(message: string): void {
    this.errorMessage.set(message);
    this.state.set('error');
  }

  reset(): void {
    this.errorMessage.set('');
    this.state.set('idle');
    this.scannerEnabled.set(true);
  }
}
