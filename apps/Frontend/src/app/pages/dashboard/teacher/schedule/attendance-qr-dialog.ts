import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
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

import { attendanceQrDialogStyles } from './attendance-qr-dialog.variants';

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
        [class]="styles().viewToggle()"
        [value]="activeView()"
        (change)="setView($event.value)"
        hideSingleSelectionIndicator
      >
        <mat-button-toggle value="qr" [class]="styles().viewToggleButton()">QR</mat-button-toggle>
        <mat-button-toggle value="roster" [class]="styles().viewToggleButton()">
          Asistencia ({{ roster().length }})
        </mat-button-toggle>
      </mat-button-toggle-group>
    }

    <mat-dialog-content [class]="styles().content()">
      @if (!isHandset() || activeView() === 'qr') {
        <section [class]="styles().qrPane()">
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
          <p [class]="styles().hint()">Los estudiantes deben escanear este código.</p>
        </section>
      }

      @if (!isHandset() || activeView() === 'roster') {
        <section [class]="styles().rosterPane()">
          <header [class]="styles().rosterHead()">
            <span class="t-small">Asistentes</span>
            <span [class]="styles().rosterCount()">{{ roster().length }}</span>
          </header>
          @if (roster().length === 0) {
            <p [class]="styles().empty()">Aún no hay asistencias.</p>
          } @else {
            <mat-list [class]="styles().rosterList()">
              @for (entry of roster(); track entry.studentId) {
                <mat-list-item>
                  <span matListItemTitle>{{ entry.studentName }}</span>
                  <span matListItemLine class="t-small">
                    {{ entry.classDate | date: 'shortDate' }}
                  </span>
                  @if (isNew(entry.studentId)) {
                    <span matListItemMeta [class]="styles().badge()">Nuevo</span>
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
  host: { class: 'block' },
})
export class AttendanceQrDialog implements OnInit, OnDestroy {
  readonly dialogRef = inject(MatDialogRef<AttendanceQrDialog>);
  readonly data = inject<AttendanceQrDialogData>(MAT_DIALOG_DATA);
  private readonly authService = inject(AuthService);
  private readonly classKindStrategies = inject(ClassKindStrategies);
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly styles = computed(() =>
    attendanceQrDialogStyles({ split: !this.isHandset() }),
  );

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
