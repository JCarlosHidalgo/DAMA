import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { QueryClient } from '@tanstack/query-core';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

import { CourseApi } from '@core/api';
import { Course } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import {
  CourseColorChip,
  EmptyState,
  ErrorState,
  Icon,
  LoadingSkeleton,
  PageHead,
  ResponsiveTable,
  type ResponsiveTableColumn,
  TableCell,
} from '@shared/components';
import { NoPasswordManager } from '@shared/directives';
import { FieldError } from '@shared/forms';

import { courseDialogStyles, coursesStyles } from './courses.variants';
import { noDotValidator } from './courses.validators';
import { coursesSubtitle, resolveCourseCreate, resolveCourseEdit } from './courses.logic';

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
    NoPasswordManager,
    FieldError,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Nuevo curso' : 'Editar curso' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" autocomplete="off" />
          <mat-error><app-field-error [control]="form.controls.name" /></mat-error>
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CourseDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<CourseDialog, string>);
  readonly data = inject<CourseDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = courseDialogStyles();

  readonly form = this.formBuilder.nonNullable.group({
    name: [this.data.name, [Validators.required, Validators.maxLength(128), noDotValidator]],
  });
}

@Component({
  selector: 'app-courses',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatTooltipModule,
    Icon,
    PageHead,
    LoadingSkeleton,
    EmptyState,
    ErrorState,
    CourseColorChip,
    ResponsiveTable,
    TableCell,
  ],
  template: `
    <app-page-head title="Cursos" [subtitle]="subtitle()">
      <button actions mat-flat-button color="primary" (click)="onCreate()">
        <app-icon name="plus" /><span [class]="styles.buttonLabel()">Nuevo curso</span>
      </button>
    </app-page-head>

    <mat-card [class]="styles.listCard()">
      <mat-card-content [class]="styles.cardContent()">
        @if (coursesQuery.isPending()) {
          <div [class]="styles.skelStack()">
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
          </div>
        } @else if (coursesQuery.isError()) {
          <app-error-state message="No se pudieron cargar los cursos.">
            <button action mat-stroked-button (click)="coursesQuery.refetch()">Reintentar</button>
          </app-error-state>
        } @else if (courses().length === 0) {
          <app-empty-state icon="chalkboard" message="No hay cursos." />
        } @else {
          <app-responsive-table [columns]="tableColumns" [rows]="courses()">
            <ng-template appTableCell="name" let-course>
              <app-course-color-chip [courseId]="course.id" [name]="course.name" />
            </ng-template>
            <ng-template appTableCell="actions" let-course>
              <button mat-icon-button matTooltip="Editar" (click)="onEdit(course)">
                <app-icon name="edit" />
              </button>
              <button
                mat-icon-button
                matTooltip="Eliminar"
                [class]="styles.dangerButton()"
                (click)="onDelete(course)"
              >
                <app-icon name="trash" />
              </button>
            </ng-template>
          </app-responsive-table>
        }
      </mat-card-content>
    </mat-card>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Courses {
  private readonly courseApi = inject(CourseApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  protected readonly styles = coursesStyles();
  protected readonly tableColumns: ResponsiveTableColumn[] = [
    { key: 'name', header: 'Nombre' },
    { key: 'actions', header: 'Acciones', mobileLayout: 'block' },
  ];

  protected readonly coursesQuery = injectQuery(() => ({
    queryKey: COURSES_QUERY_KEY,
    queryFn: () => firstValueFrom(this.courseApi.listCourses()),
  }));

  protected readonly courses = computed<Course[]>(() => this.coursesQuery.data() ?? []);
  protected readonly subtitle = computed(() => coursesSubtitle(this.courses().length));

  private readonly createCourse = injectMutation(() => ({
    mutationFn: (input: { name: string }) => firstValueFrom(this.courseApi.createCourse(input)),
    onSuccess: () => {
      this.notifications.success('Curso creado.');
      this.queryClient.invalidateQueries({ queryKey: COURSES_QUERY_KEY });
    },
    onError: (error) => this.notifications.errorFrom(error, 'Error al crear curso.'),
  }));

  private readonly updateCourse = injectMutation(() => ({
    mutationFn: (input: { id: string; name: string }) =>
      firstValueFrom(this.courseApi.updateCourse(input.id, { name: input.name })),
    onSuccess: () => {
      this.notifications.success('Curso actualizado.');
      this.queryClient.invalidateQueries({ queryKey: COURSES_QUERY_KEY });
    },
    onError: (error) => this.notifications.errorFrom(error, 'Error al actualizar curso.'),
  }));

  private readonly deleteCourse = injectMutation(() => ({
    mutationFn: (courseId: string) => firstValueFrom(this.courseApi.deleteCourse(courseId)),
    onSuccess: () => {
      this.notifications.success('Curso eliminado.');
      this.queryClient.invalidateQueries({ queryKey: COURSES_QUERY_KEY });
    },
    onError: (error) => this.notifications.errorFrom(error, 'Error al eliminar curso.'),
  }));

  async onCreate(): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'create', name: '' });
    const outcome = resolveCourseCreate(name);
    if (outcome.kind === 'skip') {
      return;
    }
    this.createCourse.mutate({ name: outcome.name });
  }

  async onEdit(course: Course): Promise<void> {
    const name = await this.openCourseDialog({ mode: 'edit', name: course.name });
    const outcome = resolveCourseEdit(course.name, name);
    if (outcome.kind === 'skip') {
      return;
    }
    this.updateCourse.mutate({ id: course.id, name: outcome.name });
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
