import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '@core/auth';
import { ClientSummary } from '@pages/dashboard/client/summary/summary';
import { StudentSummary } from '@pages/dashboard/student/summary/summary';
import { Placeholder } from './placeholder';

@Component({
  selector: 'app-summary-router',
  imports: [ClientSummary, StudentSummary, Placeholder],
  template: `
    @switch (auth.currentRole()) {
      @case ('Client') {
        <app-client-summary />
      }
      @case ('Student') {
        <app-student-summary />
      }
      @default {
        <app-placeholder title="Resumen" />
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SummaryRouter {
  protected readonly auth = inject(AuthService);
}
