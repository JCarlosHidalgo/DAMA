import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '@core/auth';
import { Schedule } from '@pages/dashboard/client/schedule/schedule';
import { TeacherSchedule } from '@pages/dashboard/teacher/schedule/schedule';
import { Placeholder } from './placeholder';

@Component({
  selector: 'app-schedule-router',
  imports: [Schedule, TeacherSchedule, Placeholder],
  template: `
    @switch (auth.currentRole()) {
      @case ('Client') {
        <app-client-schedule />
      }
      @case ('Teacher') {
        <app-teacher-schedule />
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
