import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { ClassGroup, Course, CourseScheduleEntry } from '@core/models';
import { NotificationService } from '@core/services';
import {
  filterEntriesByGroup,
  mergeCourses,
  missingCourseIds,
  nextWeekIndex,
  normalizeSchedule,
  resolveSelectedGroupId,
  subscriptionAllowsScheduleInteraction,
} from '@core/utils';
import { LoadingSkeleton, PageHead } from '@shared/components';
import { Calendar } from '@shared/components/calendar';
import { GroupSelect } from '@shared/components/group-select/group-select';
import { AttendanceQrDialog, AttendanceQrDialogData } from './attendance-qr-dialog';
import { scheduleSubtitle } from './schedule.logic';
import { teacherScheduleStyles } from './schedule.variants';

@Component({
  selector: 'app-teacher-schedule',
  imports: [MatCardModule, Calendar, GroupSelect, PageHead, LoadingSkeleton],
  template: `
    <app-page-head title="Mi horario" [subtitle]="scheduleSubtitle()" />

    <mat-card [class]="styles.controlsCard()">
      <mat-card-content>
        <app-group-select
          source="teacher"
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
export class TeacherSchedule {
  private readonly courseApi = inject(CourseApi);
  private readonly matDialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly authService = inject(AuthService);

  protected readonly styles = teacherScheduleStyles();
  protected readonly interactable = computed(() =>
    subscriptionAllowsScheduleInteraction(this.authService.effectiveSubscriptionIndex()),
  );
  protected readonly scheduleSubtitle = computed(() => scheduleSubtitle(this.interactable()));

  protected readonly entries = signal<CourseScheduleEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly selectedGroupId = signal<string>('');
  private readonly courses = signal<Course[]>([]);
  private readonly weekIndex = signal(0);
  protected readonly anchorDate = signal<string | null>(null);

  protected readonly filteredEntries = computed<CourseScheduleEntry[]>(() =>
    filterEntriesByGroup(this.entries(), this.selectedGroupId()),
  );

  constructor() {
    this.reload();
  }

  protected onGroupsLoaded(groups: ClassGroup[]): void {
    this.selectedGroupId.set(resolveSelectedGroupId(this.selectedGroupId(), groups));
  }

  protected onGroupChange(groupId: string): void {
    this.selectedGroupId.set(groupId);
  }

  protected async onWeekDelta(delta: number): Promise<void> {
    const target = nextWeekIndex(this.weekIndex(), delta);
    await this.reload(false, target);
  }

  private async reload(showSkeleton = true, weekIndexOverride?: number): Promise<void> {
    const targetWeek = weekIndexOverride ?? this.weekIndex();
    if (showSkeleton) {
      this.loading.set(true);
    }
    try {
      const scheduleResponse = await firstValueFrom(this.courseApi.getTeacherSchedule(targetWeek));
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
    this.matDialog.open<AttendanceQrDialog, AttendanceQrDialogData>(AttendanceQrDialog, {
      data: { entry },
      width: '720px',
      maxWidth: '95vw',
    });
  }
}
