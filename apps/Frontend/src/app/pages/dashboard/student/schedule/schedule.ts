import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { AttendanceApi, CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { ClassGroup, Course, CourseScheduleEntry } from '@core/models';
import { NotificationService } from '@core/services';
import {
  AttendanceMarkedDialog,
  filterEntriesByGroup,
  mergeCourses,
  missingCourseIds,
  nextWeekIndex,
  normalizeSchedule,
  resolveSelectedGroupId,
  scheduledAttendanceKey,
  subscriptionAllowsScheduleInteraction,
} from '@core/utils';
import { LoadingSkeleton, PageHead } from '@shared/components';
import { Calendar } from '@shared/components/calendar';
import { GroupSelectContainer } from '@shared/components/group-select/group-select-container';

import { ConfirmAttendanceDialog, ConfirmAttendanceDialogData } from './confirm-attendance-dialog';
import { isEntryAlreadyMarked, studentScheduleSubtitle } from './schedule.logic';
import { studentScheduleStyles } from './schedule.variants';

@Component({
  selector: 'app-student-schedule',
  imports: [MatCardModule, Calendar, GroupSelectContainer, PageHead, LoadingSkeleton],
  template: `
    <app-page-head title="Horario" [subtitle]="scheduleSubtitle()" />

    <mat-card [class]="styles.controlsCard()">
      <mat-card-content>
        <app-group-select-container
          [selectedGroupId]="selectedGroupId()"
          (groupChange)="onGroupChange($event)"
          (groupsLoaded)="onGroupsLoaded($event)"
        />
      </mat-card-content>
    </mat-card>

    <mat-card [class]="styles.calCard()">
      <mat-card-content [class]="styles.calCardContent()">
        @if (loading()) {
          <app-loading-skeleton [height]="480" />
        } @else {
          @defer {
            <app-calendar
              [entries]="filteredEntries()"
              [anchorDate]="anchorDate()"
              [tenantTimezone]="authService.tenantTimezone()"
              (eventClick)="onEvent($event)"
              (weekDelta)="onWeekDelta($event)"
            />
          } @placeholder {
            <app-loading-skeleton [height]="480" />
          }
        }
      </mat-card-content>
    </mat-card>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentSchedule {
  private readonly courseApi = inject(CourseApi);
  private readonly attendanceApi = inject(AttendanceApi);
  protected readonly authService = inject(AuthService);
  private readonly matDialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  protected readonly styles = studentScheduleStyles();
  protected readonly entries = signal<CourseScheduleEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly selectedGroupId = signal<string>('');
  private readonly courses = signal<Course[]>([]);
  private readonly weekIndex = signal(0);
  protected readonly anchorDate = signal<string | null>(null);
  private readonly markedScheduledKeys = signal<Set<string>>(new Set());
  private readonly markedUniqueIds = signal<Set<string>>(new Set());

  protected readonly interactable = computed(() =>
    subscriptionAllowsScheduleInteraction(this.authService.effectiveSubscriptionIndex()),
  );
  protected readonly scheduleSubtitle = computed(() =>
    studentScheduleSubtitle(this.interactable()),
  );

  protected readonly filteredEntries = computed<CourseScheduleEntry[]>(() =>
    filterEntriesByGroup(this.entries(), this.selectedGroupId()),
  );

  constructor() {
    this.reload();
    this.loadMarkedAttendance();
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

  private isAlreadyMarked(entry: CourseScheduleEntry): boolean {
    return isEntryAlreadyMarked(entry, this.markedScheduledKeys(), this.markedUniqueIds());
  }

  protected onGroupsLoaded(groups: ClassGroup[]): void {
    this.selectedGroupId.set(resolveSelectedGroupId(this.selectedGroupId(), groups));
  }

  protected onGroupChange(groupId: string): void {
    this.selectedGroupId.set(groupId);
  }

  protected async onWeekDelta(delta: number): Promise<void> {
    await this.reload(false, nextWeekIndex(this.weekIndex(), delta));
  }

  private async reload(showSkeleton = true, weekIndexOverride?: number): Promise<void> {
    const targetWeek = weekIndexOverride ?? this.weekIndex();
    if (showSkeleton) {
      this.loading.set(true);
    }
    try {
      const scheduleResponse = await firstValueFrom(this.courseApi.getStudentSchedule(targetWeek));
      await this.ensureCourseNames(scheduleResponse);
      this.weekIndex.set(targetWeek);
      this.anchorDate.set(scheduleResponse.weekStartDate);
      this.entries.set(normalizeSchedule(scheduleResponse, this.courses()));
    } catch {
      this.notifications.error('Error al cargar horario.');
      this.entries.set([]);
    } finally {
      if (showSkeleton) {
        this.loading.set(false);
      }
    }
  }

  private async ensureCourseNames(scheduleResponse: {
    scheduledClasses?: { courseId: string }[];
    uniqueClasses?: { courseId: string }[];
  }): Promise<void> {
    const missingIds = missingCourseIds(scheduleResponse, this.courses());
    if (missingIds.length === 0) {
      return;
    }

    const fetchedCourses = await Promise.all(
      missingIds.map(async (courseId) => {
        try {
          return await firstValueFrom(this.courseApi.getCourse(courseId));
        } catch {
          return null;
        }
      }),
    );
    this.courses.set(mergeCourses(this.courses(), fetchedCourses));
  }

  onEvent(entry: CourseScheduleEntry): void {
    if (!this.interactable()) {
      return;
    }
    if (this.isAlreadyMarked(entry)) {
      this.matDialog.open(AttendanceMarkedDialog, { width: '380px', maxWidth: '95vw' });
      return;
    }

    const dialogRef = this.matDialog.open<
      ConfirmAttendanceDialog,
      ConfirmAttendanceDialogData,
      boolean
    >(ConfirmAttendanceDialog, {
      data: { entry },
      width: '420px',
      maxWidth: '95vw',
    });

    dialogRef.afterClosed().subscribe((marked) => {
      if (marked) {
        this.rememberMarked(entry);
      }
    });
  }

  private rememberMarked(entry: CourseScheduleEntry): void {
    if (entry.classKind === 'Scheduled') {
      this.markedScheduledKeys.update((keys) =>
        new Set(keys).add(scheduledAttendanceKey(entry.classId, entry.date)),
      );
      return;
    }
    this.markedUniqueIds.update((ids) => new Set(ids).add(entry.classId));
  }
}
