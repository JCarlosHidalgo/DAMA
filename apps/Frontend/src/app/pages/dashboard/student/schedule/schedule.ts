import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { AttendanceApi, CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { ClassGroup, Course, CourseScheduleEntry } from '@core/models';
import { NotificationService } from '@core/services';
import { AttendanceMarkedDialog, normalizeSchedule } from '@core/utils';
import { LoadingSkeleton, PageHead } from '@shared/components';
import { Calendar } from '@shared/components/calendar';
import { GroupSelect } from '@shared/components/group-select/group-select';

import { ConfirmAttendanceDialog, ConfirmAttendanceDialogData } from './confirm-attendance-dialog';

@Component({
  selector: 'app-student-schedule',
  imports: [MatCardModule, Calendar, GroupSelect, PageHead, LoadingSkeleton],
  template: `
    <app-page-head title="Horario" [subtitle]="scheduleSubtitle()" />

    <mat-card class="controls-card">
      <mat-card-content>
        <app-group-select
          [selectedGroupId]="selectedGroupId()"
          (groupChange)="onGroupChange($event)"
          (groupsLoaded)="onGroupsLoaded($event)"
        />
      </mat-card-content>
    </mat-card>

    <mat-card class="cal-card">
      <mat-card-content>
        @if (loading()) {
          <app-loading-skeleton [height]="480" />
        } @else {
          @defer {
            <app-calendar
              [entries]="filteredEntries()"
              [anchorDate]="anchorDate()"
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
  styles: `
    :host {
      display: block;
    }
    .controls-card {
      margin-bottom: 12px;
    }
    .cal-card {
      padding: 0;
    }
    .cal-card mat-card-content {
      padding: 12px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentSchedule {
  private readonly courseApi = inject(CourseApi);
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly authService = inject(AuthService);
  private readonly matDialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  protected readonly entries = signal<CourseScheduleEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly selectedGroupId = signal<string>('');
  private readonly courses = signal<Course[]>([]);
  private readonly weekIndex = signal(0);
  protected readonly anchorDate = signal<string | null>(null);
  private readonly markedScheduledKeys = signal<Set<string>>(new Set());
  private readonly markedUniqueIds = signal<Set<string>>(new Set());

  protected readonly interactable = computed(
    () => this.authService.effectiveSubscriptionIndex() >= 2,
  );
  protected readonly scheduleSubtitle = computed(() =>
    this.interactable() ? 'Toca una clase para confirmar tu asistencia' : 'Vista de solo lectura',
  );

  protected readonly filteredEntries = computed<CourseScheduleEntry[]>(() => {
    const groupId = this.selectedGroupId();
    if (!groupId) {
      return this.entries();
    }
    return this.entries().filter((entry) => entry.groupId === groupId);
  });

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
        new Set(scheduled.map((attendance) => `${attendance.classId}|${attendance.classDate}`)),
      );
      this.markedUniqueIds.set(new Set(unique.map((attendance) => attendance.classId)));
    } catch {
      this.markedScheduledKeys.set(new Set());
      this.markedUniqueIds.set(new Set());
    }
  }

  private isAlreadyMarked(entry: CourseScheduleEntry): boolean {
    if (entry.classKind === 'Scheduled') {
      return this.markedScheduledKeys().has(`${entry.classId}|${entry.date}`);
    }
    return this.markedUniqueIds().has(entry.classId);
  }

  protected onGroupsLoaded(groups: ClassGroup[]): void {
    if (!this.selectedGroupId() || !groups.some((group) => group.id === this.selectedGroupId())) {
      this.selectedGroupId.set(groups[0]?.id ?? '');
    }
  }

  protected onGroupChange(groupId: string): void {
    this.selectedGroupId.set(groupId);
  }

  protected async onWeekDelta(delta: number): Promise<void> {
    const nextWeekIndex = delta === 0 ? 0 : this.weekIndex() + delta;
    await this.reload(false, nextWeekIndex);
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
    const knownIds = new Set(this.courses().map((course) => course.id));
    const missingIds = new Set<string>();
    for (const scheduledClass of scheduleResponse.scheduledClasses ?? []) {
      if (!knownIds.has(scheduledClass.courseId)) {
        missingIds.add(scheduledClass.courseId);
      }
    }
    for (const uniqueClass of scheduleResponse.uniqueClasses ?? []) {
      if (!knownIds.has(uniqueClass.courseId)) {
        missingIds.add(uniqueClass.courseId);
      }
    }
    if (missingIds.size === 0) {
      return;
    }

    const fetchedCourses = await Promise.all(
      Array.from(missingIds).map(async (courseId) => {
        try {
          return await firstValueFrom(this.courseApi.getCourse(courseId));
        } catch {
          return null;
        }
      }),
    );
    const mergedCourses = [...this.courses()];
    for (const course of fetchedCourses) {
      if (course) {
        mergedCourses.push(course);
      }
    }
    this.courses.set(mergedCourses);
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
        new Set(keys).add(`${entry.classId}|${entry.date}`),
      );
      return;
    }
    this.markedUniqueIds.update((ids) => new Set(ids).add(entry.classId));
  }
}
