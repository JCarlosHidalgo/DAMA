import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { catchError, forkJoin, map, of, startWith } from 'rxjs';

import { AttendanceApi, PaymentApi } from '@core/api';
import { AuthService } from '@core/auth';
import { StudentRemainClasses } from '@core/models';
import { ErrorState, Icon, LoadingSkeleton, PageHead } from '@shared/components';
import { BarChart, LineChart } from '@shared/components/charts';
import { MoneyPipe } from '@shared/pipes';

import { aggregateClassesPerMonth, MonthlySeries, spendPointsToLine } from './summary.logic';
import { studentSummaryStyles } from './summary.variants';

type RemainState =
  | { kind: 'loading' }
  | { kind: 'ready'; data: StudentRemainClasses }
  | { kind: 'error' };

const EMPTY_SERIES: MonthlySeries = { labels: [], values: [] };

@Component({
  selector: 'app-student-summary',
  imports: [MatCardModule, Icon, PageHead, LoadingSkeleton, ErrorState, LineChart, BarChart],
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

    <div [class]="chartsGrid">
      @if (spend().values.length) {
        <app-line-chart
          title="Gasto por mes"
          seriesLabel="Gasto"
          [labels]="spend().labels"
          [values]="spend().values"
          [area]="true"
          colorKey="primary"
          [valueFormatter]="moneyFormatter"
        />
      }
      @if (classesPerMonth().values.length) {
        <app-bar-chart
          title="Clases asistidas por mes"
          seriesLabel="Clases"
          [labels]="classesPerMonth().labels"
          [values]="classesPerMonth().values"
          colorKey="success"
        />
      }
    </div>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentSummary {
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly paymentApi = inject(PaymentApi);
  private readonly auth = inject(AuthService);

  private readonly studentId = this.auth.claims()?.userId ?? '';

  protected readonly chartsGrid = 'mt-grid grid grid-cols-1 gap-grid lg:grid-cols-2';

  protected readonly styles = computed(() =>
    studentSummaryStyles({ zero: this.ready()?.numberOfClasses === 0 }),
  );

  protected readonly moneyFormatter = (value: number): string => new MoneyPipe().transform(value);

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

  protected readonly spend = toSignal(
    this.paymentApi.getStudentSpend().pipe(
      map((points) => spendPointsToLine(points)),
      catchError(() => of(EMPTY_SERIES)),
    ),
    { initialValue: EMPTY_SERIES },
  );

  protected readonly classesPerMonth = toSignal(
    forkJoin({
      scheduled: this.attendanceApi.myScheduledHistory(this.studentId),
      unique: this.attendanceApi.myUniqueHistory(this.studentId),
    }).pipe(
      map(({ scheduled, unique }) => aggregateClassesPerMonth(scheduled, unique)),
      catchError(() => of(EMPTY_SERIES)),
    ),
    { initialValue: EMPTY_SERIES },
  );
}
