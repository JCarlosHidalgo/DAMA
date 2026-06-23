import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { firstValueFrom } from 'rxjs';

import { AuthApi, CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { ClassGroup, Course, CourseScheduleEntry, UserListItem } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { ClassKindStrategies } from '@core/strategies';
import {
  filterEntriesByGroup,
  isoWeekdayIndex,
  nextWeekIndex as nextWeekIndexFn,
  normalizeSchedule,
  resolveSelectedGroupId,
} from '@core/utils';
import { Icon, LoadingSkeleton, PageHead } from '@shared/design/components';
import { Calendar } from '@shared/design/components/calendar';
import { GroupSelectContainer } from '@shared/design/components/group-select/group-select-container';
import { NoPasswordManager } from '@shared/directives';

import {
  buildTeacherPayload,
  candidateGroups as candidateGroupsFn,
  classifyTransferError,
  ClassDialogResult,
  DAY_OF_WEEK_OPTIONS,
  deleteClassMessage,
  entriesForGroupAndDay,
  findGroupById,
  FormKind,
  formKindForClassKind,
  isValidClassForm,
  kindLabel,
  resolveTargetGroupId,
  resolveDayDelta,
  teacherNames,
  transferConfirmMessage,
} from './schedule.logic';

import { scheduleClassTagStyles, scheduleDialogStyles, scheduleStyles } from './schedule.variants';

interface ClassDialogData {
  mode: 'create' | 'edit';
  kind: FormKind;
  courses: Course[];
  teachers: UserListItem[];
  groups: ClassGroup[];
  initial?: Partial<ClassDialogResult> & { id?: string };
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
    NoPasswordManager,
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'create' ? 'Nueva clase' : 'Editar clase' }}
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" [class]="styles.form()">
        @if (data.mode === 'create') {
          <mat-button-toggle-group formControlName="kind">
            <mat-button-toggle value="scheduled">Recurrente</mat-button-toggle>
            <mat-button-toggle value="unique">Única</mat-button-toggle>
          </mat-button-toggle-group>

          <mat-form-field appearance="outline" [class]="styles.field()">
            <mat-label>Curso</mat-label>
            <mat-select formControlName="courseId">
              @for (course of data.courses; track course.id) {
                <mat-option [value]="course.id">{{ course.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" [class]="styles.field()">
            <mat-label>Grupo</mat-label>
            <mat-select formControlName="groupId">
              @for (group of data.groups; track group.id) {
                <mat-option [value]="group.id">{{ group.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }

        @if (form.controls.kind.value === 'scheduled') {
          <mat-form-field appearance="outline" [class]="styles.field()">
            <mat-label>Día de la semana</mat-label>
            <mat-select formControlName="dayOfWeekIndex">
              @for (day of dayOptions; track day.value) {
                <mat-option [value]="day.value">{{ day.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        } @else {
          <mat-form-field appearance="outline" [class]="styles.field()">
            <mat-label>Fecha</mat-label>
            <input matInput type="date" formControlName="date" />
          </mat-form-field>
        }

        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Inicio</mat-label>
          <input matInput type="time" formControlName="startTime" />
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Fin</mat-label>
          <input matInput type="time" formControlName="endTime" />
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Límite de estudiantes</mat-label>
          <input matInput type="number" min="0" max="1000" formControlName="maxStudentLimit" />
          <mat-hint>0 = sin límite</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<ScheduleDialog, ClassDialogResult>);
  readonly data = inject<ClassDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = scheduleDialogStyles();
  protected readonly dayOptions = DAY_OF_WEEK_OPTIONS;

  protected readonly form = this.formBuilder.nonNullable.group({
    kind: [this.data.kind as FormKind],
    courseId: [this.data.initial?.courseId ?? '', Validators.required],
    groupId: [this.data.initial?.groupId ?? '', Validators.required],
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
    if (!isValidClassForm(formValue)) {
      return;
    }
    this.dialogRef.close({
      kind: formValue.kind,
      courseId: formValue.courseId,
      groupId: formValue.groupId,
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
    DragDropModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatTooltipModule,
    Calendar,
    GroupSelectContainer,
    Icon,
    PageHead,
    LoadingSkeleton,
  ],
  template: `
    <app-page-head title="Horario" />

    <mat-card [class]="styles().controlsCard()">
      <mat-card-content [class]="styles().controls()">
        <app-group-select-container
          [editable]="true"
          [locked]="transferMode()"
          [selectedGroupId]="selectedGroupId()"
          (groupChange)="onGroupChange($event)"
          (groupsLoaded)="onGroupsLoaded($event)"
        />
        @if (groups().length >= 2) {
          <button mat-stroked-button (click)="toggleTransfer()">
            <app-icon name="transfer" />
            <span [class]="styles().buttonLabel()">{{
              transferMode() ? 'Cerrar transferencia' : 'Transferir clases'
            }}</span>
          </button>
        }
      </mat-card-content>
    </mat-card>

    <div [class]="styles().columns()">
      <mat-card [class]="styles().colCard()">
        <mat-card-content>
          <div [class]="styles().colHead()">
            <h3 [class]="styles().colTitle()">{{ selectedGroup()?.name ?? 'Grupo' }}</h3>
            <div [class]="styles().colHeadActions()">
              <button
                mat-flat-button
                color="primary"
                (click)="onCreate()"
                [disabled]="courses().length === 0 || groups().length === 0"
              >
                <app-icon name="plus" /><span [class]="styles().buttonLabel()">Nueva clase</span>
              </button>
              <mat-form-field
                appearance="outline"
                subscriptSizing="dynamic"
                [class]="styles().daySelect()"
              >
                <mat-label>Día</mat-label>
                <mat-select
                  [value]="selectedDayIndex()"
                  (valueChange)="selectedDayIndex.set($event)"
                >
                  @for (day of dayOptions; track day.value) {
                    <mat-option [value]="day.value">{{ day.label }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>
          </div>
          @if (loading()) {
            <app-loading-skeleton [height]="320" />
          } @else {
            <div
              cdkDropList
              id="group-source-list"
              [cdkDropListData]="selectedGroupId()"
              [cdkDropListConnectedTo]="transferMode() ? ['group-target-list'] : []"
              (cdkDropListDropped)="onDrop($event)"
              [class]="styles().classList()"
            >
              @for (entry of selectedGroupListEntries(); track entry.classId) {
                <div
                  [class]="styles().classItem()"
                  cdkDrag
                  [cdkDragData]="entry"
                  [cdkDragDisabled]="!transferMode()"
                >
                  <div [class]="styles().classMain()">
                    <span [class]="styles().classCourse()">
                      {{ entry.courseName }}
                      <span [class]="tagClass(entry)">{{ kindLabel(entry) }}</span>
                    </span>
                    <span [class]="styles().classTime()">
                      <app-icon name="clock" />
                      {{ entry.startTime.slice(0, 5) }} – {{ entry.endTime.slice(0, 5) }}
                    </span>
                    <span [class]="styles().classTeachers()">{{ teacherNames(entry) }}</span>
                  </div>
                  @if (!transferMode()) {
                    <div class="class-actions">
                      <button mat-icon-button matTooltip="Editar" (click)="onEdit(entry)">
                        <app-icon name="edit" />
                      </button>
                      <button
                        mat-icon-button
                        matTooltip="Eliminar"
                        [class]="styles().dangerButton()"
                        (click)="onDelete(entry)"
                      >
                        <app-icon name="trash" />
                      </button>
                    </div>
                  }
                </div>
              } @empty {
                <p [class]="styles().empty()">No hay clases en este grupo.</p>
              }
            </div>
          }
        </mat-card-content>
      </mat-card>

      @if (transferMode()) {
        <mat-card [class]="styles().colCard()">
          <mat-card-content>
            <div [class]="styles().colHead()">
              <mat-form-field
                appearance="outline"
                subscriptSizing="dynamic"
                [class]="styles().groupSelectField()"
              >
                <mat-label>Grupo candidato</mat-label>
                <mat-select [value]="targetGroupId()" (valueChange)="targetGroupId.set($event)">
                  @for (group of candidateGroupsComputed(); track group.id) {
                    <mat-option [value]="group.id">{{ group.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field
                appearance="outline"
                subscriptSizing="dynamic"
                [class]="styles().daySelect()"
              >
                <mat-label>Día</mat-label>
                <mat-select [value]="targetDayIndex()" (valueChange)="targetDayIndex.set($event)">
                  @for (day of dayOptions; track day.value) {
                    <mat-option [value]="day.value">{{ day.label }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>
            <div
              cdkDropList
              id="group-target-list"
              [cdkDropListData]="targetGroupId()"
              [cdkDropListConnectedTo]="['group-source-list']"
              (cdkDropListDropped)="onDrop($event)"
              [class]="styles().classList()"
            >
              @for (entry of targetGroupListEntries(); track entry.classId) {
                <div [class]="styles().classItem()" cdkDrag [cdkDragData]="entry">
                  <div [class]="styles().classMain()">
                    <span [class]="styles().classCourse()">
                      {{ entry.courseName }}
                      <span [class]="tagClass(entry)">{{ kindLabel(entry) }}</span>
                    </span>
                    <span [class]="styles().classTime()">
                      <app-icon name="clock" />
                      {{ entry.startTime.slice(0, 5) }} – {{ entry.endTime.slice(0, 5) }}
                    </span>
                    <span [class]="styles().classTeachers()">{{ teacherNames(entry) }}</span>
                  </div>
                </div>
              } @empty {
                <p [class]="styles().empty()">No hay clases en este grupo.</p>
              }
            </div>
          </mat-card-content>
        </mat-card>
      }
    </div>

    <mat-card [class]="styles().calCard()">
      <mat-card-content [class]="styles().calCardContent()">
        @if (loading()) {
          <app-loading-skeleton [height]="480" />
        } @else {
          @defer {
            <app-calendar
              [entries]="selectedGroupEntries()"
              [anchorDate]="anchorDate()"
              [selectedDayIndex]="selectedDayIndex()"
              [tenantTimezone]="authService.tenantTimezone()"
              (weekDelta)="onWeekDelta($event)"
              (dayDelta)="onDayDelta($event)"
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
export class Schedule {
  private readonly courseApi = inject(CourseApi);
  private readonly authApi = inject(AuthApi);
  protected readonly authService = inject(AuthService);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly classKindStrategies = inject(ClassKindStrategies);

  protected readonly styles = computed(() => scheduleStyles({ split: this.transferMode() }));
  protected readonly courses = signal<Course[]>([]);
  protected readonly teachers = signal<UserListItem[]>([]);
  protected readonly entries = signal<CourseScheduleEntry[]>([]);
  protected readonly loading = signal(true);
  protected readonly groups = signal<ClassGroup[]>([]);
  protected readonly selectedGroupId = signal<string>('');
  protected readonly transferMode = signal(false);
  protected readonly selectedDayIndex = signal<number>(1);
  protected readonly targetGroupId = signal<string>('');
  protected readonly targetDayIndex = signal<number>(1);
  protected readonly dayOptions = DAY_OF_WEEK_OPTIONS;
  private readonly weekIndex = signal(0);
  private dayDefaultApplied = false;

  protected readonly anchorDate = signal<string | null>(null);

  protected readonly selectedGroup = computed<ClassGroup | undefined>(() =>
    findGroupById(this.groups(), this.selectedGroupId()),
  );

  protected readonly candidateGroupsComputed = computed<ClassGroup[]>(() =>
    candidateGroupsFn(this.groups(), this.selectedGroupId()),
  );

  protected readonly targetGroup = computed<ClassGroup | undefined>(() =>
    findGroupById(this.groups(), this.targetGroupId()),
  );

  protected readonly selectedGroupEntries = computed<CourseScheduleEntry[]>(() =>
    filterEntriesByGroup(this.entries(), this.selectedGroupId()),
  );

  protected readonly selectedGroupListEntries = computed<CourseScheduleEntry[]>(() =>
    entriesForGroupAndDay(this.entries(), this.selectedGroupId(), this.selectedDayIndex()),
  );

  protected readonly targetGroupListEntries = computed<CourseScheduleEntry[]>(() => {
    const target = this.targetGroup();
    if (!target) {
      return [];
    }
    return entriesForGroupAndDay(this.entries(), target.id, this.targetDayIndex());
  });

  protected readonly kindLabel = kindLabel;
  protected readonly teacherNames = teacherNames;

  constructor() {
    this.initialLoad();
  }

  protected onGroupsLoaded(groups: ClassGroup[]): void {
    this.groups.set(groups);
    this.selectedGroupId.set(resolveSelectedGroupId(this.selectedGroupId(), groups));
    this.ensureValidTargetGroup();
  }

  protected onGroupChange(groupId: string): void {
    this.selectedGroupId.set(groupId);
    this.ensureValidTargetGroup();
  }

  protected toggleTransfer(): void {
    this.transferMode.update((active) => !active);
    if (this.transferMode()) {
      this.ensureValidTargetGroup();
    }
  }

  protected tagClass(entry: CourseScheduleEntry): string {
    return scheduleClassTagStyles({ kind: entry.classKind === 'Scheduled' ? 'weekly' : 'unique' });
  }

  private ensureValidTargetGroup(): void {
    this.targetGroupId.set(
      resolveTargetGroupId(this.candidateGroupsComputed(), this.targetGroupId()),
    );
  }

  private applyTodayDayDefault(todayDate: string): void {
    if (this.dayDefaultApplied) {
      return;
    }
    this.dayDefaultApplied = true;
    const todayWeekday = isoWeekdayIndex(todayDate);
    this.selectedDayIndex.set(todayWeekday);
    this.targetDayIndex.set(todayWeekday);
  }

  protected async onDrop(dropEvent: CdkDragDrop<string>): Promise<void> {
    const targetGroupId = dropEvent.container.data;
    const sourceGroupId = dropEvent.previousContainer.data;
    if (targetGroupId === sourceGroupId) {
      return;
    }
    const entry = dropEvent.item.data as CourseScheduleEntry;
    const targetGroup = this.groups().find((group) => group.id === targetGroupId);
    const confirmed = await this.dialogs.confirm({
      title: 'Transferir clase',
      message: transferConfirmMessage(entry.courseName, targetGroup?.name ?? ''),
      confirmLabel: 'Transferir',
    });
    if (!confirmed) {
      return;
    }
    try {
      await firstValueFrom(
        entry.classKind === 'Scheduled'
          ? this.courseApi.transferScheduledClass(entry.classId, targetGroupId)
          : this.courseApi.transferUniqueClass(entry.classId, targetGroupId),
      );
      this.notifications.success('Clase transferida.');
      await this.reloadSchedule();
    } catch (error: unknown) {
      this.notifications.error(classifyTransferError(error));
    }
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
    await this.reloadSchedule(false, nextWeekIndexFn(this.weekIndex(), delta));
  }

  protected async onDayDelta(delta: number): Promise<void> {
    const outcome = resolveDayDelta(this.selectedDayIndex(), this.weekIndex(), delta);
    this.selectedDayIndex.set(outcome.dayIndex);
    if (outcome.reload) {
      await this.reloadSchedule(false, outcome.weekIndex);
    }
  }

  private async reloadSchedule(showSkeleton = true, weekIndexOverride?: number): Promise<void> {
    const targetWeek = weekIndexOverride ?? this.weekIndex();
    if (showSkeleton) {
      this.loading.set(true);
    }
    try {
      const scheduleResponse = await firstValueFrom(this.courseApi.getTenantSchedule(targetWeek));
      this.weekIndex.set(targetWeek);
      this.anchorDate.set(scheduleResponse.weekStartDate);
      this.applyTodayDayDefault(scheduleResponse.todayDate);
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

  async onCreate(): Promise<void> {
    const result = await this.openClassDialog({
      mode: 'create',
      kind: 'scheduled',
      courses: this.courses(),
      teachers: this.teachers(),
      groups: this.groups(),
      initial: {
        courseId: this.courses()[0]?.id ?? '',
        groupId: this.selectedGroupId() || (this.groups()[0]?.id ?? ''),
        dayOfWeekIndex: this.selectedDayIndex(),
      },
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
          groupId: result.groupId,
          teachers: buildTeacherPayload(result.teacherIds, this.teachers()),
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
    const formKind = formKindForClassKind(entry.classKind);
    const dayOfWeekIndex = entry.dayOfWeekIndex ?? 1;
    const result = await this.openClassDialog({
      mode: 'edit',
      kind: formKind,
      courses: this.courses(),
      teachers: this.teachers(),
      groups: this.groups(),
      initial: {
        kind: formKind,
        courseId: entry.courseId,
        groupId: entry.groupId,
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
          groupId: entry.groupId,
          teachers: buildTeacherPayload(result.teacherIds, this.teachers()),
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
      message: deleteClassMessage(entry.courseName, entry.date),
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
}
