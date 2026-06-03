import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { catchError, map, of, startWith } from 'rxjs';

import { AttendanceApi } from '@core/api';
import { StudentRemainClasses } from '@core/models';
import { ErrorState, Icon, LoadingSkeleton, PageHead } from '@shared/components';

import { studentSummaryStyles } from './summary.variants';

type RemainState =
  | { kind: 'loading' }
  | { kind: 'ready'; data: StudentRemainClasses }
  | { kind: 'error' };

@Component({
  selector: 'app-student-summary',
  imports: [MatCardModule, Icon, PageHead, LoadingSkeleton, ErrorState],
  template: `
    <app-page-head title="Tu saldo" subtitle="Clases restantes para asistir." />

    @switch (state().kind) {
      @case ('loading') {
        <app-loading-skeleton [height]="220" width="480px" />
      }
      @case ('error') {
        <app-error-state message="No se pudo cargar tu saldo." />
      }
      @case ('ready') {
        @if (ready(); as remainStatus) {
          <mat-card [class]="styles().remain()">
            <mat-card-content>
              <div [class]="styles().head()">
                <span class="t-label-up">Clases restantes</span>
                <span class="t-small">{{ remainStatus.studentName ?? 'Tu cuenta' }}</span>
              </div>
              <div [class]="styles().count()">
                <app-icon
                  [class]="styles().countIcon()"
                  [name]="remainStatus.numberOfClasses === 0 ? 'ban' : 'calendar-check'"
                />
                <span class="t-num-xl tabular-nums">{{ remainStatus.numberOfClasses }}</span>
              </div>
              <p class="t-small">
                @if (remainStatus.numberOfClasses === 0) {
                  No tienes clases disponibles. Compra un paquete para continuar.
                } @else {
                  Cada asistencia descuenta una clase de este saldo.
                }
              </p>
            </mat-card-content>
          </mat-card>
        }
      }
    }
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentSummary {
  private readonly attendanceApi = inject(AttendanceApi);

  protected readonly styles = computed(() =>
    studentSummaryStyles({ zero: this.ready()?.numberOfClasses === 0 }),
  );

  readonly state = toSignal(
    this.attendanceApi.getMyRemain().pipe(
      map((data): RemainState => ({ kind: 'ready', data })),
      startWith<RemainState>({ kind: 'loading' }),
      catchError(() => of<RemainState>({ kind: 'error' })),
    ),
    { initialValue: { kind: 'loading' } as RemainState },
  );

  readonly ready = computed<StudentRemainClasses | null>(() => {
    const currentState = this.state();
    return currentState.kind === 'ready' ? currentState.data : null;
  });
}
