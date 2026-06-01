import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { catchError, map, of, startWith } from 'rxjs';

import { AttendanceApi } from '@core/api';
import { StudentRemainClasses } from '@core/models';
import { ErrorState, Icon, LoadingSkeleton, PageHead } from '@shared/components';

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
          <mat-card class="remain" [class.zero]="remainStatus.numberOfClasses === 0">
            <mat-card-content>
              <div class="head">
                <span class="t-label-up">Clases restantes</span>
                <span class="t-small">{{ remainStatus.studentName ?? 'Tu cuenta' }}</span>
              </div>
              <div class="count">
                <app-icon [name]="remainStatus.numberOfClasses === 0 ? 'ban' : 'calendar-check'" />
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
  styles: `
    :host {
      display: block;
    }

    .remain {
      max-width: 480px;
      margin: 0 auto;
      position: relative;
    }
    .remain.zero {
      border-left: 4px solid var(--dama-danger);
    }

    .head {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 8px;
    }
    .count {
      display: flex;
      align-items: center;
      gap: 16px;
      margin: 12px 0 16px;
      app-icon {
        font-size: 40px;
        color: var(--dama-text-muted);
      }
    }
    .remain.zero app-icon {
      color: var(--dama-danger);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentSummary {
  private readonly attendanceApi = inject(AttendanceApi);

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
