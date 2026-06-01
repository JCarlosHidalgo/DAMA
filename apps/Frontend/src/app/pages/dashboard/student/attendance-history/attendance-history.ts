import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';

import { AttendanceApi } from '@core/api';
import { ScheduledClassAttendance, UniqueClassAttendance } from '@core/models';
import { NotificationService } from '@core/services';
import { PaginatedTabState } from '@core/utils';
import { EmptyState, LoadingSkeleton, PageHead, Paginator } from '@shared/components';
import { TenantDatePipe } from '@shared/pipes';

type TabKind = 'scheduled' | 'unique';

@Component({
  selector: 'app-attendance-history',
  imports: [
    MatCardModule,
    MatTabsModule,
    MatTableModule,
    PageHead,
    Paginator,
    LoadingSkeleton,
    EmptyState,
    TenantDatePipe,
  ],
  template: `
    <app-page-head
      title="Mis asistencias"
      subtitle="Historial de clases donde marcaste asistencia."
    />

    <mat-card class="tabs-card">
      <mat-card-content>
        <mat-tab-group (selectedIndexChange)="onTabChange($event)">
          <mat-tab label="Recurrentes">
            <ng-template matTabContent>
              @if (scheduled.state().loading) {
                <div class="skel-stack">
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                </div>
              } @else if (!scheduled.hasItems()) {
                <app-empty-state
                  icon="calendar-check"
                  message="Aún no tienes asistencias registradas."
                />
              } @else {
                <div class="table-wrap">
                  <table mat-table [dataSource]="scheduled.state().page!.items" class="full">
                    <ng-container matColumnDef="course">
                      <th mat-header-cell *matHeaderCellDef>Nombre de curso</th>
                      <td mat-cell *matCellDef="let attendance">{{ attendance.courseName }}</td>
                    </ng-container>
                    <ng-container matColumnDef="date">
                      <th mat-header-cell *matHeaderCellDef>Fecha</th>
                      <td mat-cell *matCellDef="let attendance" class="tabular-nums">
                        {{ attendance.classDate | tenantDate }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="time">
                      <th mat-header-cell *matHeaderCellDef>Horario</th>
                      <td mat-cell *matCellDef="let attendance" class="tabular-nums">
                        {{ formatTimeRange(attendance.startTime, attendance.endTime) }}
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="columns"></tr>
                    <tr mat-row *matRowDef="let row; columns: columns"></tr>
                  </table>
                </div>
                <div class="paginator-wrap">
                  <app-paginator
                    [page]="{
                      currentIndex: scheduled.state().pageIndex,
                      maxIndex: scheduled.state().page!.maxIndex,
                    }"
                    (pageChange)="changePage('scheduled', $event)"
                  />
                </div>
              }
            </ng-template>
          </mat-tab>

          <mat-tab label="Únicas">
            <ng-template matTabContent>
              @if (unique.state().loading) {
                <div class="skel-stack">
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                </div>
              } @else if (!unique.hasItems()) {
                <app-empty-state
                  icon="calendar-check"
                  message="Aún no tienes asistencias a clases únicas."
                />
              } @else {
                <div class="table-wrap">
                  <table mat-table [dataSource]="unique.state().page!.items" class="full">
                    <ng-container matColumnDef="course">
                      <th mat-header-cell *matHeaderCellDef>Nombre de curso</th>
                      <td mat-cell *matCellDef="let attendance">{{ attendance.courseName }}</td>
                    </ng-container>
                    <ng-container matColumnDef="date">
                      <th mat-header-cell *matHeaderCellDef>Fecha</th>
                      <td mat-cell *matCellDef="let attendance" class="tabular-nums">
                        {{ attendance.classDate | tenantDate }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="time">
                      <th mat-header-cell *matHeaderCellDef>Horario</th>
                      <td mat-cell *matCellDef="let attendance" class="tabular-nums">
                        {{ formatTimeRange(attendance.startTime, attendance.endTime) }}
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="columns"></tr>
                    <tr mat-row *matRowDef="let row; columns: columns"></tr>
                  </table>
                </div>
                <div class="paginator-wrap">
                  <app-paginator
                    [page]="{
                      currentIndex: unique.state().pageIndex,
                      maxIndex: unique.state().page!.maxIndex,
                    }"
                    (pageChange)="changePage('unique', $event)"
                  />
                </div>
              }
            </ng-template>
          </mat-tab>
        </mat-tab-group>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    :host {
      display: block;
    }
    .tabs-card {
      padding: 0;
    }
    .tabs-card mat-card-content {
      padding: 0;
    }
    .skel-stack {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 20px;
    }
    .table-wrap {
      overflow-x: auto;
    }
    .full {
      width: 100%;
    }
    .paginator-wrap {
      display: flex;
      justify-content: center;
      padding: 16px;
      border-top: 1px solid var(--dama-divider);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceHistory {
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = ['course', 'date', 'time'];

  protected readonly scheduled = new PaginatedTabState<ScheduledClassAttendance>();
  protected readonly unique = new PaginatedTabState<UniqueClassAttendance>();

  constructor() {
    this.loadTab('scheduled', 0);
  }

  onTabChange(index: number): void {
    const kind: TabKind = index === 0 ? 'scheduled' : 'unique';
    const tab = kind === 'scheduled' ? this.scheduled : this.unique;
    if (!tab.state().page) {
      this.loadTab(kind, 0);
    }
  }

  async changePage(kind: TabKind, pageIndex: number): Promise<void> {
    await this.loadTab(kind, pageIndex);
  }

  private async loadTab(kind: TabKind, pageIndex: number): Promise<void> {
    try {
      if (kind === 'scheduled') {
        await this.scheduled.loadFrom(
          (page) => this.attendanceApi.listMyScheduledAttendance(page),
          pageIndex,
        );
      } else {
        await this.unique.loadFrom(
          (page) => this.attendanceApi.listMyUniqueAttendance(page),
          pageIndex,
        );
      }
    } catch {
      this.notifications.error('Error al cargar asistencias.');
    }
  }

  protected formatTimeRange(start: string, end: string): string {
    return `${start.slice(0, 5)} – ${end.slice(0, 5)}`;
  }
}
