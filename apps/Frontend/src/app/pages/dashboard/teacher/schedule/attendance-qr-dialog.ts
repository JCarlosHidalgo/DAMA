import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { Subscription } from 'rxjs';

import { AuthService } from '@core/auth';
import { CourseScheduleEntry } from '@core/models';
import { ClassKindStrategies, RosterEntry } from '@core/strategies';
import { encodeQr } from '@core/utils';
import { LoadingSkeleton } from '@shared/components';
import { QrCard } from '@shared/components/qr-card/qr-card';

export interface AttendanceQrDialogData {
  entry: CourseScheduleEntry;
}

type ActiveView = 'qr' | 'roster';

@Component({
  selector: 'app-attendance-qr-dialog',
  imports: [
    DatePipe,
    MatDialogModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatListModule,
    QrCard,
    LoadingSkeleton,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Tomar asistencia</h2>

    @if (isHandset()) {
      <mat-button-toggle-group
        class="view-toggle"
        [value]="activeView()"
        (change)="setView($event.value)"
        hideSingleSelectionIndicator
      >
        <mat-button-toggle value="qr">QR</mat-button-toggle>
        <mat-button-toggle value="roster"> Asistencia ({{ roster().length }}) </mat-button-toggle>
      </mat-button-toggle-group>
    }

    <mat-dialog-content [class.split]="!isHandset()">
      @if (!isHandset() || activeView() === 'qr') {
        <section class="qr-pane">
          @defer {
            <app-qr-card
              [payload]="qrData"
              [title]="data.entry.courseName"
              [subtitle]="
                data.entry.date + ' · ' + data.entry.startTime + ' – ' + data.entry.endTime
              "
              [size]="260"
            />
          } @placeholder {
            <app-loading-skeleton [height]="260" />
          }
          <p class="hint t-small">Los estudiantes deben escanear este código.</p>
        </section>
      }

      @if (!isHandset() || activeView() === 'roster') {
        <section class="roster-pane">
          <header class="roster-head">
            <span class="t-small">Asistentes</span>
            <span class="count">{{ roster().length }}</span>
          </header>
          @if (roster().length === 0) {
            <p class="empty t-small">Aún no hay asistencias.</p>
          } @else {
            <mat-list class="roster-list">
              @for (entry of roster(); track entry.studentId) {
                <mat-list-item>
                  <span matListItemTitle>{{ entry.studentName }}</span>
                  <span matListItemLine class="t-small">
                    {{ entry.classDate | date: 'shortDate' }}
                  </span>
                  @if (isNew(entry.studentId)) {
                    <span matListItemMeta class="badge">Nuevo</span>
                  }
                </mat-list-item>
              }
            </mat-list>
          }
        </section>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: `
    :host {
      display: block;
    }
    .view-toggle {
      display: flex;
      margin: 0 24px 8px;
    }
    .view-toggle mat-button-toggle {
      flex: 1;
    }
    mat-dialog-content.split {
      display: flex;
      gap: 24px;
      align-items: stretch;
    }
    .qr-pane {
      display: flex;
      flex-direction: column;
      align-items: center;
      flex: 0 0 auto;
    }
    .hint {
      text-align: center;
      color: var(--dama-text-muted);
      margin: 12px 0 0;
    }
    .roster-pane {
      flex: 1 1 280px;
      min-width: 240px;
      display: flex;
      flex-direction: column;
      max-height: 340px;
    }
    .roster-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 4px 4px 8px;
      border-bottom: 1px solid var(--mat-sys-outline-variant, rgba(0, 0, 0, 0.12));
    }
    .roster-head .count {
      font-weight: 600;
    }
    .empty {
      color: var(--dama-text-muted);
      text-align: center;
      padding: 16px 0;
    }
    .roster-list {
      overflow-y: auto;
      flex: 1;
      padding: 0;
    }
    .badge {
      background: var(--mat-sys-primary-container, #d0e4ff);
      color: var(--mat-sys-on-primary-container, #001b3d);
      border-radius: 10px;
      padding: 2px 8px;
      font-size: 11px;
      font-weight: 600;
      animation: fadeOut 4s forwards;
    }
    @keyframes fadeOut {
      0%,
      60% {
        opacity: 1;
      }
      100% {
        opacity: 0;
      }
    }
  `,
})
export class AttendanceQrDialog implements OnInit, OnDestroy {
  readonly dialogRef = inject(MatDialogRef<AttendanceQrDialog>);
  readonly data = inject<AttendanceQrDialogData>(MAT_DIALOG_DATA);
  private readonly authService = inject(AuthService);
  private readonly classKindStrategies = inject(ClassKindStrategies);
  private readonly breakpoints = inject(BreakpointObserver);

  private streamSubscription?: Subscription;
  private breakpointSubscription?: Subscription;
  private readonly badgeTimers = new Set<ReturnType<typeof setTimeout>>();
  private readonly recentIds = signal<Set<string>>(new Set());
  private readonly newBadgeMilliseconds = 4000;

  private readonly strategy = this.classKindStrategies.for(this.data.entry.classKind);

  protected readonly qrData = encodeQr({
    tenantId: this.authService.claims()?.tenantId ?? '',
    courseName: this.data.entry.courseName,
    kind: this.data.entry.classKind === 'Scheduled' ? 'SCHEDULED' : 'UNIQUE',
    classId: this.data.entry.classId,
  });

  protected readonly roster = signal<RosterEntry[]>([]);
  protected readonly isHandset = signal(false);
  protected readonly activeView = signal<ActiveView>('qr');

  ngOnInit(): void {
    this.breakpointSubscription = this.breakpoints
      .observe([Breakpoints.HandsetPortrait, Breakpoints.HandsetLandscape])
      .subscribe((state) => this.isHandset.set(state.matches));

    this.loadInitialRoster();
    this.startStream();
  }

  ngOnDestroy(): void {
    this.streamSubscription?.unsubscribe();
    this.breakpointSubscription?.unsubscribe();
    for (const timerHandle of this.badgeTimers) {
      clearTimeout(timerHandle);
    }
    this.badgeTimers.clear();
  }

  protected setView(view: ActiveView): void {
    this.activeView.set(view);
  }

  protected isNew(studentId: string): boolean {
    return this.recentIds().has(studentId);
  }

  private loadInitialRoster(): void {
    this.strategy
      .fetchRoster({
        classId: this.data.entry.classId,
        courseName: this.data.entry.courseName,
        classDate: this.data.entry.date,
      })
      .subscribe({
        next: (rows) => this.mergeRoster(rows, false),
        error: () => {
          // initial load failure is non-fatal; live stream will still populate.
        },
      });
  }

  private startStream(): void {
    this.streamSubscription = this.strategy
      .connectRealtime({
        classId: this.data.entry.classId,
        courseName: this.data.entry.courseName,
        classDate: this.data.entry.date,
      })
      .subscribe({
        next: (row) => this.mergeRoster([row], true),
      });
  }

  private mergeRoster(rows: RosterEntry[], markAsNew: boolean): void {
    if (rows.length === 0) {
      return;
    }
    const current = this.roster();
    const knownIds = new Set(current.map((row) => row.studentId));
    const additions = rows.filter((row) => !knownIds.has(row.studentId));
    if (additions.length === 0) {
      return;
    }
    this.roster.set([...current, ...additions]);

    if (markAsNew) {
      const recents = new Set(this.recentIds());
      for (const addition of additions) {
        recents.add(addition.studentId);
        const timerHandle = setTimeout(() => {
          this.badgeTimers.delete(timerHandle);
          const nextRecents = new Set(this.recentIds());
          nextRecents.delete(addition.studentId);
          this.recentIds.set(nextRecents);
        }, this.newBadgeMilliseconds);
        this.badgeTimers.add(timerHandle);
      }
      this.recentIds.set(recents);
    }
  }
}
