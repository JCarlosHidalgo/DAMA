import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';

import { AuthApi, CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { Course, CourseScheduleEntry, UserListItem } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { ClassKindStrategies } from '@core/strategies';
import { normalizeSchedule, nowInTenant } from '@core/utils';
import { Calendar, Icon, LoadingSkeleton, PageHead } from '@shared/components';

type FormKind = 'scheduled' | 'unique';

interface ClassDialogData {
  mode: 'create' | 'edit';
  kind: FormKind;
  courses: Course[];
  teachers: UserListItem[];
  initial?: Partial<ClassDialogResult> & { id?: string };
}

interface ClassDialogResult {
  kind: FormKind;
  courseId: string;
  teacherIds: string[];
  dayOfWeekIndex: number;
  date: string;
  startTime: string;
  endTime: string;
  maxStudentLimit: number;
}

@Component({
  selector: 'app-schedule-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatButtonToggleModule,
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'create' ? 'Nueva clase' : 'Editar clase' }}
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        @if (data.mode === 'create') {
          <mat-button-toggle-group formControlName="kind">
            <mat-button-toggle value="scheduled">Recurrente</mat-button-toggle>
            <mat-button-toggle value="unique">Única</mat-button-toggle>
          </mat-button-toggle-group>

          <mat-form-field appearance="outline">
            <mat-label>Curso</mat-label>
            <mat-select formControlName="courseId">
              @for (course of data.courses; track course.id) {
                <mat-option [value]="course.id">{{ course.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }

        @if (form.controls.kind.value === 'scheduled') {
          <mat-form-field appearance="outline">
            <mat-label>Día de la semana</mat-label>
            <mat-select formControlName="dayOfWeekIndex">
              @for (day of dayOptions; track day.value) {
                <mat-option [value]="day.value">{{ day.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        } @else {
          <mat-form-field appearance="outline">
            <mat-label>Fecha</mat-label>
            <input matInput type="date" formControlName="date" />
          </mat-form-field>
        }

        <mat-form-field appearance="outline">
          <mat-label>Inicio</mat-label>
          <input matInput type="time" formControlName="startTime" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Fin</mat-label>
          <input matInput type="time" formControlName="endTime" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Límite de estudiantes</mat-label>
          <input matInput type="number" min="0" max="1000" formControlName="maxStudentLimit" />
          <mat-hint>0 = sin límite</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Profesores</mat-label>
          <mat-select formControlName="teacherIds" multiple>
            @for (teacher of data.teachers; track teacher.id) {
              <mat-option [value]="teacher.id">{{ teacher.username }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="submit()">
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      min-width: 360px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<ScheduleDialog, ClassDialogResult>);
  readonly data = inject<ClassDialogData>(MAT_DIALOG_DATA);

  protected readonly dayOptions = [
    { value: 1, label: 'Lunes' },
    { value: 2, label: 'Martes' },
    { value: 3, label: 'Miércoles' },
    { value: 4, label: 'Jueves' },
    { value: 5, label: 'Viernes' },
    { value: 6, label: 'Sábado' },
    { value: 7, label: 'Domingo' },
  ];

  protected readonly form = this.formBuilder.nonNullable.group({
    kind: [this.data.kind as FormKind],
    courseId: [this.data.initial?.courseId ?? '', Validators.required],
    teacherIds: [this.data.initial?.teacherIds ?? ([] as string[])],
    dayOfWeekIndex: [this.data.initial?.dayOfWeekIndex ?? 1],
    date: [this.data.initial?.date ?? ''],
    startTime: [this.data.initial?.startTime ?? '09:00', Validators.required],
    endTime: [this.data.initial?.endTime ?? '10:00', Validators.required],
    maxStudentLimit: [
      this.data.initial?.maxStudentLimit ?? 0,
      [Validators.required, Validators.min(0), Validators.max(1000)],
    ],
  });

  submit(): void {
    const formValue = this.form.getRawValue();
    if (formValue.startTime >= formValue.endTime) {
      return;
    }
    if (formValue.kind === 'unique' && !formValue.date) {
      return;
    }
    this.dialogRef.close({
      kind: formValue.kind,
      courseId: formValue.courseId,
      teacherIds: formValue.teacherIds,
      dayOfWeekIndex: formValue.dayOfWeekIndex,
      date: formValue.date,
      startTime: formValue.startTime,
      endTime: formValue.endTime,
      maxStudentLimit: formValue.maxStudentLimit,
    });
  }
}

@Component({
  selector: 'app-client-schedule',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    Calendar,
    Icon,
    PageHead,
    LoadingSkeleton,
  ],
  template: `
    <app-page-head title="Horario">
      <button
        actions
        mat-flat-button
        color="primary"
        (click)="onCreate()"
        [disabled]="courses().length === 0"
      >
        <app-icon name="plus" /><span class="btn-label">Nueva clase</span>
      </button>
    </app-page-head>

    <mat-card class="cal-card">
      <mat-card-content>
        @if (loading()) {
          <app-loading-skeleton [height]="480" />
        } @else {
          @defer {
            <app-calendar
              [entries]="entries()"
              [editable]="true"
              (editClick)="onEdit($event)"
              (deleteClick)="onDelete($event)"
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
    .cal-card {
      padding: 0;
    }
    .cal-card mat-card-content {
      padding: 12px;
    }
    .btn-label {
      margin-left: 6px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Schedule {
  private readonly courseApi = inject(CourseApi);
  private readonly authApi = inject(AuthApi);
  private readonly authService = inject(AuthService);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly classKindStrategies = inject(ClassKindStrategies);

  protected readonly courses = signal<Course[]>([]);
  protected readonly teachers = signal<UserListItem[]>([]);
  protected readonly entries = signal<CourseScheduleEntry[]>([]);
  protected readonly loading = signal(true);
  private readonly weekIndex = signal(0);

  constructor() {
    this.initialLoad();
  }

  private async initialLoad(): Promise<void> {
    this.loading.set(true);
    try {
      const [courses, teachers] = await Promise.all([
        firstValueFrom(this.courseApi.listCourses()),
        this.loadAllTeachers(),
      ]);
      this.courses.set(courses ?? []);
      this.teachers.set(teachers);
      await this.reloadSchedule();
    } catch {
      this.notifications.error('Error al cargar datos iniciales.');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadAllTeachers(): Promise<UserListItem[]> {
    const accumulatedTeachers: UserListItem[] = [];
    let pageIndex = 0;
    while (true) {
      const page = await firstValueFrom(this.authApi.listTeachers(pageIndex));
      accumulatedTeachers.push(...page.items);
      if (pageIndex >= page.maxPageIndex) {
        break;
      }
      pageIndex++;
    }
    return accumulatedTeachers;
  }

  protected async onWeekDelta(delta: number): Promise<void> {
    const nextWeekIndex = delta === 0 ? 0 : this.weekIndex() + delta;
    this.weekIndex.set(nextWeekIndex);
    await this.reloadSchedule();
  }

  private async reloadSchedule(): Promise<void> {
    this.loading.set(true);
    try {
      const scheduleResponse = await firstValueFrom(
        this.courseApi.getTenantSchedule(this.weekIndex()),
      );
      const today = nowInTenant(this.authService.tenantTimezone());
      this.entries.set(
        normalizeSchedule(scheduleResponse, this.weekIndex(), this.courses(), today),
      );
    } catch {
      this.notifications.error('Error al cargar horario.');
      this.entries.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async onCreate(): Promise<void> {
    const result = await this.openClassDialog({
      mode: 'create',
      kind: 'scheduled',
      courses: this.courses(),
      teachers: this.teachers(),
      initial: { courseId: this.courses()[0]?.id ?? '' },
    });
    if (!result) {
      return;
    }
    try {
      const strategy = this.classKindStrategies.for(
        result.kind === 'scheduled' ? 'Scheduled' : 'Unique',
      );
      await firstValueFrom(
        strategy.create({
          courseId: result.courseId,
          teachers: this.buildTeacherPayload(result.teacherIds),
          startTime: result.startTime,
          endTime: result.endTime,
          dayOfWeekIndex: result.dayOfWeekIndex,
          date: result.date,
          maxStudentLimit: result.maxStudentLimit,
        }),
      );
      this.notifications.success('Clase creada.');
      await this.reloadSchedule();
    } catch {
      this.notifications.error('Error al crear clase.');
    }
  }

  async onEdit(entry: CourseScheduleEntry): Promise<void> {
    const formKind: FormKind = entry.classKind === 'Scheduled' ? 'scheduled' : 'unique';
    const dayOfWeekIndex = entry.dayOfWeekIndex ?? 1;
    const result = await this.openClassDialog({
      mode: 'edit',
      kind: formKind,
      courses: this.courses(),
      teachers: this.teachers(),
      initial: {
        kind: formKind,
        courseId: entry.courseId,
        teacherIds: entry.teachers.map((teacher) => teacher.teacherId),
        dayOfWeekIndex,
        date: entry.date,
        startTime: entry.startTime.slice(0, 5),
        endTime: entry.endTime.slice(0, 5),
        maxStudentLimit: entry.maxStudentLimit,
      },
    });
    if (!result) {
      return;
    }
    try {
      const strategy = this.classKindStrategies.for(entry.classKind);
      await firstValueFrom(
        strategy.update(entry.classId, {
          courseId: entry.courseId,
          teachers: this.buildTeacherPayload(result.teacherIds),
          startTime: result.startTime,
          endTime: result.endTime,
          dayOfWeekIndex: result.dayOfWeekIndex,
          date: result.date,
          maxStudentLimit: result.maxStudentLimit,
        }),
      );
      this.notifications.success('Clase actualizada.');
      await this.reloadSchedule();
    } catch {
      this.notifications.error('Error al actualizar clase.');
    }
  }

  async onDelete(entry: CourseScheduleEntry): Promise<void> {
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar clase',
      message: `¿Eliminar clase de ${entry.courseName} el ${entry.date}?`,
      destructive: true,
      confirmLabel: 'Eliminar',
    });
    if (!confirmed) {
      return;
    }
    try {
      await firstValueFrom(this.classKindStrategies.for(entry.classKind).delete(entry.classId));
      this.notifications.success('Clase eliminada.');
      await this.reloadSchedule();
    } catch {
      this.notifications.error('Error al eliminar clase.');
    }
  }

  private openClassDialog(data: ClassDialogData): Promise<ClassDialogResult | undefined> {
    return this.dialogs.openForm<ScheduleDialog, ClassDialogData, ClassDialogResult>(
      ScheduleDialog,
      data,
      { width: '480px' },
    );
  }

  private buildTeacherPayload(teacherIds: string[]): { teacherId: string; teacherName: string }[] {
    const teacherNameById = new Map(
      this.teachers().map((teacher) => [teacher.id, teacher.username]),
    );
    return teacherIds.map((teacherId) => ({
      teacherId,
      teacherName: teacherNameById.get(teacherId) ?? teacherId,
    }));
  }
}
