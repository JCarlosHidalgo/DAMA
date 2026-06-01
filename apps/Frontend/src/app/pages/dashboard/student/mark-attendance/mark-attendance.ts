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
import { AttendanceMarkedDialog, decodeQr, todayDateOnlyInTenant } from '@core/utils';
import { Icon, LoadingSkeleton, PageHead } from '@shared/components';
import { CameraScanner } from '@shared/components/camera-scanner/camera-scanner';

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

    <mat-card class="scanner-card">
      <mat-card-content>
        @if (remain() !== null && remain() === 0) {
          <div class="zero-state">
            <app-icon name="ban" />
            <p class="t-h2">No tienes clases disponibles.</p>
            <p class="t-small">Compra un paquete antes de marcar asistencia.</p>
          </div>
        } @else {
          @defer {
            <app-camera-scanner [enabled]="scannerEnabled()" (scanned)="onScan($event)" />
          } @placeholder {
            <app-loading-skeleton [height]="320" />
          }

          @switch (state()) {
            @case ('submitting') {
              <div class="status">
                <mat-spinner diameter="24" />
                <span class="t-body">Registrando...</span>
              </div>
            }
            @case ('success') {
              <div class="status ok">
                <app-icon name="check" />
                <span class="t-body">Asistencia registrada.</span>
              </div>
            }
            @case ('error') {
              <div class="status err">
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
  styles: `
    :host {
      display: block;
    }
    .scanner-card {
      max-width: 600px;
      margin: 0 auto;
    }

    .status {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
      padding: 16px;
      margin-top: 14px;
      border-radius: var(--dama-radius-sm);
      flex-wrap: wrap;

      &.ok {
        background: var(--dama-success-soft);
        color: var(--dama-success);
      }
      &.err {
        background: var(--dama-danger-soft);
        color: var(--dama-danger);
      }
    }

    .zero-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 6px;
      padding: 36px 20px;
      text-align: center;
      background: var(--dama-danger-soft);
      border: 1px solid color-mix(in oklab, var(--dama-danger) 25%, transparent);
      border-radius: var(--dama-radius-md);
      app-icon {
        font-size: 48px;
        color: var(--dama-danger);
        margin-bottom: 8px;
      }
      p {
        margin: 0;
        color: var(--dama-text);
      }
      .t-small {
        color: var(--dama-text-muted);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarkAttendance {
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly classKindStrategies = inject(ClassKindStrategies);
  private readonly matDialog = inject(MatDialog);
  private readonly router = inject(Router);

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
        new Set(scheduled.map((attendance) => `${attendance.classId}|${attendance.classDate}`)),
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

    const payload = decodeQr(rawQrText);
    if (!payload) {
      this.fail('Código QR no válido.');
      return;
    }

    const expectedTenantId = this.authService.claims()?.tenantId ?? '';
    if (payload.tenantId !== expectedTenantId) {
      this.fail('Este QR no corresponde a tu cuenta.');
      return;
    }

    const kind = payload.kind === 'SCHEDULED' ? 'Scheduled' : 'Unique';
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
      if (error instanceof Error && error.message.includes('AlreadyMarked')) {
        this.showAlreadyMarked();
        return;
      }
      const userMessage =
        error instanceof Error && error.message.includes('OutsideAllowedWindow')
          ? 'Fuera del horario permitido (01:00–23:00 local).'
          : 'No se pudo registrar la asistencia.';
      this.fail(userMessage);
    }
  }

  private isAlreadyMarked(kind: 'Scheduled' | 'Unique', classId: string): boolean {
    if (kind === 'Scheduled') {
      const today = todayDateOnlyInTenant(this.authService.tenantTimezone());
      return this.markedScheduledKeys().has(`${classId}|${today}`);
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
