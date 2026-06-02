import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { ClassGroup, Course, CourseScheduleEntry } from '@core/models';
import { NotificationService } from '@core/services';
import { normalizeSchedule, nowInTenant, weekAnchorIsoDate } from '@core/utils';
import { LoadingSkeleton, PageHead } from '@shared/components';
import { Calendar } from '@shared/components/calendar';
import { GroupSelect } from '@shared/components/group-select/group-select';
import { AttendanceQrDialog, AttendanceQrDialogData } from './attendance-qr-dialog';

@Component({
  selector: 'app-teacher-schedule',
  imports: [MatCardModule, Calendar, GroupSelect, PageHead, LoadingSkeleton],
  template: `
    <app-page-head title="Mi horario" subtitle="Toca una clase para abrir el QR de asistencia" />

    <mat-card class="controls-card">
      <mat-card-content>
        <app-group-select
          source="teacher"
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
export class TeacherSchedule {
  private readonly courseApi = inject(CourseApi);
  private readonly authService = inject(AuthService);
  private readonly matDialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  protected readonly entries = signal<CourseScheduleEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly selectedGroupId = signal<string>('');
  private readonly courses = signal<Course[]>([]);
  private readonly weekIndex = signal(0);
  protected readonly anchorDate = computed(() =>
    weekAnchorIsoDate(nowInTenant(this.authService.tenantTimezone()), this.weekIndex()),
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
    this.weekIndex.set(nextWeekIndex);
    await this.reload(false);
  }

  private async reload(showSkeleton = true): Promise<void> {
    if (showSkeleton) {
      this.loading.set(true);
    }
    try {
      const scheduleResponse = await firstValueFrom(
        this.courseApi.getTeacherSchedule(this.weekIndex()),
      );
      await this.ensureCourseNames(scheduleResponse);
      const today = nowInTenant(this.authService.tenantTimezone());
      this.entries.set(
        normalizeSchedule(scheduleResponse, this.weekIndex(), this.courses(), today),
      );
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
    this.matDialog.open<AttendanceQrDialog, AttendanceQrDialogData>(AttendanceQrDialog, {
      data: { entry },
      width: '720px',
      maxWidth: '95vw',
    });
  }
}
