import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '@core/auth';
import { Schedule } from '@pages/dashboard/client/schedule/schedule';
import { TeacherSchedule } from '@pages/dashboard/teacher/schedule/schedule';
import { StudentSchedule } from '@pages/dashboard/student/schedule/schedule';
import { Placeholder } from './placeholder';

@Component({
  selector: 'app-schedule-router',
  imports: [Schedule, TeacherSchedule, StudentSchedule, Placeholder],
  template: `
    @switch (auth.currentRole()) {
      @case ('Client') {
        <app-client-schedule />
      }
      @case ('Teacher') {
        <app-teacher-schedule />
      }
      @case ('Student') {
        <app-student-schedule />
      }
      @default {
        <app-placeholder title="Horario" />
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleRouter {
  protected readonly auth = inject(AuthService);
}
