import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { QueryClient } from '@tanstack/query-core';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

import { CourseApi } from '@core/api';
import { ClassGroup } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { Icon } from '@shared/components/icon';
import { NoPasswordManager } from '@shared/directives';

import { groupNameDialogStyles, groupSelectStyles } from './group-select.variants';

export const GROUPS_QUERY_KEY = ['class-groups'] as const;
export const TEACHER_GROUPS_QUERY_KEY = ['teacher-class-groups'] as const;

export type GroupSource = 'tenant' | 'teacher';

interface GroupNameDialogData {
  mode: 'create' | 'rename';
  name: string;
}

@Component({
  selector: 'app-group-name-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    NoPasswordManager,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Nuevo grupo' : 'Renombrar grupo' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" autocomplete="off" />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
          @if (form.controls.name.hasError('maxlength')) {
            <mat-error>Máximo 128 caracteres</mat-error>
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupNameDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<GroupNameDialog, string>);
  readonly data = inject<GroupNameDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = groupNameDialogStyles();

  readonly form = this.formBuilder.nonNullable.group({
    name: [this.data.name, [Validators.required, Validators.maxLength(128)]],
  });
}

/**
 * Group selector with optional inline create/rename/delete actions for the Client.
 */
@Component({
  selector: 'app-group-select',
  imports: [MatFormFieldModule, MatSelectModule, MatButtonModule, MatTooltipModule, Icon],
  template: `
    <div [class]="styles.root()">
      <mat-form-field appearance="outline" [class]="styles.field()" subscriptSizing="dynamic">
        <mat-label>Grupo</mat-label>
        <mat-select
          [value]="selectedGroupId()"
          (valueChange)="groupChange.emit($event)"
          [disabled]="locked() || groups().length === 0"
        >
          @for (group of groups(); track group.id) {
            <mat-option [value]="group.id">{{ group.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      @if (editable()) {
        <div [class]="styles.actions()">
          <button
            mat-icon-button
            matTooltip="Nuevo grupo"
            [disabled]="locked()"
            (click)="onCreate()"
          >
            <app-icon name="plus" />
          </button>
          <button
            mat-icon-button
            matTooltip="Renombrar grupo"
            [disabled]="locked() || !selectedGroup()"
            (click)="onRename()"
          >
            <app-icon name="edit" />
          </button>
          <button
            mat-icon-button
            matTooltip="Eliminar grupo"
            [class]="styles.dangerButton()"
            [disabled]="locked() || !selectedGroup()"
            (click)="onDelete()"
          >
            <app-icon name="trash" />
          </button>
        </div>
      }
    </div>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupSelect {
  private readonly courseApi = inject(CourseApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  readonly editable = input<boolean>(false);
  readonly locked = input<boolean>(false);
  readonly selectedGroupId = input<string>('');
  readonly source = input<GroupSource>('tenant');

  readonly groupChange = output<string>();
  readonly groupsLoaded = output<ClassGroup[]>();
  protected readonly styles = groupSelectStyles();

  protected readonly groupsQuery = injectQuery(() => ({
    queryKey: this.source() === 'teacher' ? TEACHER_GROUPS_QUERY_KEY : GROUPS_QUERY_KEY,
    queryFn: () =>
      firstValueFrom(
        this.source() === 'teacher'
          ? this.courseApi.getTeacherGroups()
          : this.courseApi.getGroups(),
      ),
  }));

  protected readonly groups = computed<ClassGroup[]>(() => this.groupsQuery.data() ?? []);
  protected readonly selectedGroup = computed<ClassGroup | undefined>(() =>
    this.groups().find((group) => group.id === this.selectedGroupId()),
  );

  private readonly createGroup = injectMutation(() => ({
    mutationFn: (name: string) => firstValueFrom(this.courseApi.createGroup(name)),
    onSuccess: (created: ClassGroup) => {
      this.notifications.success('Grupo creado.');
      this.queryClient.invalidateQueries({ queryKey: GROUPS_QUERY_KEY });
      this.groupChange.emit(created.id);
    },
    onError: () => this.notifications.error('Error al crear grupo.'),
  }));

  private readonly renameGroup = injectMutation(() => ({
    mutationFn: (input: { id: string; name: string }) =>
      firstValueFrom(this.courseApi.renameGroup(input.id, input.name)),
    onSuccess: () => {
      this.notifications.success('Grupo actualizado.');
      this.queryClient.invalidateQueries({ queryKey: GROUPS_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al actualizar grupo.'),
  }));

  private readonly deleteGroup = injectMutation(() => ({
    mutationFn: (groupId: string) => firstValueFrom(this.courseApi.deleteGroup(groupId)),
    onSuccess: () => {
      this.notifications.success('Grupo eliminado.');
      this.queryClient.invalidateQueries({ queryKey: GROUPS_QUERY_KEY });
    },
    onError: () => this.notifications.error('No se pudo eliminar: el grupo aún tiene clases.'),
  }));

  constructor() {
    effect(() => {
      if (this.groupsQuery.isError()) {
        this.notifications.error('Error al cargar grupos.');
      }
    });
    effect(() => {
      const loaded = this.groups();
      if (loaded.length > 0) {
        this.groupsLoaded.emit(loaded);
      }
    });
  }

  async onCreate(): Promise<void> {
    const name = await this.openNameDialog({ mode: 'create', name: '' });
    if (!name) {
      return;
    }
    this.createGroup.mutate(name);
  }

  async onRename(): Promise<void> {
    const current = this.selectedGroup();
    if (!current) {
      return;
    }
    const name = await this.openNameDialog({ mode: 'rename', name: current.name });
    if (!name || name === current.name) {
      return;
    }
    this.renameGroup.mutate({ id: current.id, name });
  }

  async onDelete(): Promise<void> {
    const current = this.selectedGroup();
    if (!current) {
      return;
    }
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar grupo',
      message: `¿Eliminar el grupo "${current.name}"? Solo es posible si no tiene clases.`,
      destructive: true,
      confirmLabel: 'Eliminar',
    });
    if (!confirmed) {
      return;
    }
    this.deleteGroup.mutate(current.id);
  }

  private openNameDialog(data: GroupNameDialogData): Promise<string | undefined> {
    return this.dialogs.openForm<GroupNameDialog, GroupNameDialogData, string>(
      GroupNameDialog,
      data,
      { width: '420px' },
    );
  }
}
