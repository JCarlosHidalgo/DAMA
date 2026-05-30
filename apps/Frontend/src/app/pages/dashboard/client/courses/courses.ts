import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { QueryClient } from '@tanstack/query-core';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

import { CourseApi } from '@core/api';
import { Course } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { CourseColorChip, EmptyState, Icon, LoadingSkeleton, PageHead } from '@shared/components';

const COURSES_QUERY_KEY = ['courses'] as const;

interface CourseDialogData {
  mode: 'create' | 'edit';
  name: string;
}

@Component({
  selector: 'app-course-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Nuevo curso' : 'Editar curso' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" autocomplete="off" />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
          @if (form.controls.name.hasError('maxlength')) {
            <mat-error>Máximo 128 caracteres</mat-error>
          }
          @if (form.controls.name.hasError('hasDot')) {
            <mat-error>No se permiten puntos (.)</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancelar</button>
      <button
        mat-flat-button
        color="primary"
        [disabled]="form.invalid"
        (click)="dialogRef.close(form.getRawValue().name.trim())"
      >
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    mat-form-field {
      width: 100%;
      min-width: 320px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CourseDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<CourseDialog, string>);
  readonly data = inject<CourseDialogData>(MAT_DIALOG_DATA);

  readonly form = this.formBuilder.nonNullable.group({
    name: [
      this.data.name,
      [
        Validators.required,
        Validators.maxLength(128),
        (control: AbstractControl) => (control.value?.includes('.') ? { hasDot: true } : null),
      ],
    ],
  });
}

@Component({
  selector: 'app-courses',
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatTooltipModule,
    Icon,
    PageHead,
    LoadingSkeleton,
    EmptyState,
    CourseColorChip,
  ],
  template: `
    <app-page-head title="Cursos" [subtitle]="subtitle()">
      <button actions mat-flat-button color="primary" (click)="onCreate()">
        <app-icon name="plus" /><span class="btn-label">Nuevo curso</span>
      </button>
    </app-page-head>

    <mat-card class="list-card">
      <mat-card-content>
        @if (coursesQuery.isPending()) {
          <div class="skel-stack">
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
          </div>
        } @else if (courses().length === 0) {
          <app-empty-state icon="chalkboard" message="No hay cursos." />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="courses()" class="full">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Nombre</th>
                <td mat-cell *matCellDef="let course">
                  <app-course-color-chip [courseId]="course.id" [name]="course.name" />
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef class="mat-column-actions">Acciones</th>
                <td mat-cell *matCellDef="let course" class="mat-column-actions">
                  <button mat-icon-button matTooltip="Editar" (click)="onEdit(course)">
                    <app-icon name="edit" />
                  </button>
                  <button
                    mat-icon-button
                    matTooltip="Eliminar"
                    class="danger-btn"
                    (click)="onDelete(course)"
                  >
                    <app-icon name="trash" />
                  </button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    :host {
      display: block;
    }
    .list-card {
      padding: 0;
    }
    .list-card mat-card-content {
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
    .danger-btn {
      color: var(--dama-danger);
    }
    .btn-label {
      margin-left: 6px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Courses {
  private readonly courseApi = inject(CourseApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  protected readonly columns = ['name', 'actions'];

  protected readonly coursesQuery = injectQuery(() => ({
    queryKey: COURSES_QUERY_KEY,
    queryFn: () => firstValueFrom(this.courseApi.listCourses()),
  }));

  protected readonly courses = computed<Course[]>(() => this.coursesQuery.data() ?? []);
  protected readonly subtitle = computed(() => `${this.courses().length} curso(s)`);

  private readonly createCourse = injectMutation(() => ({
    mutationFn: (input: { name: string }) => firstValueFrom(this.courseApi.createCourse(input)),
    onSuccess: () => {
      this.notifications.success('Curso creado.');
      this.queryClient.invalidateQueries({ queryKey: COURSES_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al crear curso.'),
  }));

  private readonly updateCourse = injectMutation(() => ({
    mutationFn: (input: { id: string; name: string }) =>
      firstValueFrom(this.courseApi.updateCourse(input.id, { name: input.name })),
    onSuccess: () => {
      this.notifications.success('Curso actualizado.');
      this.queryClient.invalidateQueries({ queryKey: COURSES_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al actualizar curso.'),
  }));

  private readonly deleteCourse = injectMutation(() => ({
    mutationFn: (courseId: string) => firstValueFrom(this.courseApi.deleteCourse(courseId)),
    onSuccess: () => {
      this.notifications.success('Curso eliminado.');
      this.queryClient.invalidateQueries({ queryKey: COURSES_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al eliminar curso.'),
  }));

  constructor() {
    effect(() => {
      if (this.coursesQuery.isError()) {
        this.notifications.error('Error al cargar cursos.');
      }
    });
  }

  async onCreate(): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'create', name: '' });
    if (!name) {
      return;
    }
    this.createCourse.mutate({ name });
  }

  async onEdit(course: Course): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'edit', name: course.name });
    if (!name || name === course.name) {
      return;
    }
    this.updateCourse.mutate({ id: course.id, name });
  }

  async onDelete(course: Course): Promise<void> {
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar curso',
      message: `¿Eliminar "${course.name}"? Se eliminarán también sus clases.`,
      destructive: true,
      confirmLabel: 'Eliminar',
    });
    if (!confirmed) {
      return;
    }
    this.deleteCourse.mutate(course.id);
  }

  private openCourseDialog(data: CourseDialogData): Promise<string | undefined> {
    return this.dialogs.openForm<CourseDialog, CourseDialogData, string>(CourseDialog, data, {
      width: '420px',
    });
  }
}
