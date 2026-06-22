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
import { QueryClient } from '@tanstack/query-core';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

import { CourseApi } from '@core/api';
import { ClassGroup } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { NoPasswordManager } from '@shared/directives';

import { GroupSelect } from './group-select';
import { groupNameDialogStyles } from './group-select.variants';
import {
  GroupSource,
  groupsQueryKey,
  resolveGroupCreate,
  resolveGroupRename,
} from './group-select.logic';

export { TEACHER_GROUPS_QUERY_KEY } from './group-select.logic';
export type { GroupSource } from './group-select.logic';

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
 * Smart container for GroupSelect: fetches groups and handles CRUD via CourseApi.
 */
@Component({
  selector: 'app-group-select-container',
  imports: [GroupSelect],
  template: `
    <app-group-select
      [groups]="groups()"
      [loading]="groupsQuery.isPending()"
      [creating]="createGroup.isPending()"
      [renaming]="renameGroup.isPending()"
      [deleting]="deleteGroup.isPending()"
      [editable]="editable()"
      [locked]="locked()"
      [selectedGroupId]="selectedGroupId()"
      [source]="source()"
      (groupChange)="groupChange.emit($event)"
      (groupsLoaded)="groupsLoaded.emit($event)"
      (createRequested)="onCreate()"
      (renameRequested)="onRename($event)"
      (deleteRequested)="onDelete($event)"
    />
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupSelectContainer {
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

  protected readonly groupsQuery = injectQuery(() => ({
    queryKey: groupsQueryKey(this.source()),
    queryFn: () =>
      firstValueFrom(
        this.source() === 'teacher'
          ? this.courseApi.getTeacherGroups()
          : this.courseApi.getGroups(),
      ),
  }));

  protected readonly groups = computed<ClassGroup[]>(() => this.groupsQuery.data() ?? []);

  protected readonly createGroup = injectMutation(() => ({
    mutationFn: (name: string) => firstValueFrom(this.courseApi.createGroup(name)),
    onSuccess: (created: ClassGroup) => {
      this.notifications.success('Grupo creado.');
      this.queryClient.invalidateQueries({ queryKey: groupsQueryKey(this.source()) });
      this.groupChange.emit(created.id);
    },
    onError: () => this.notifications.error('Error al crear grupo.'),
  }));

  protected readonly renameGroup = injectMutation(() => ({
    mutationFn: (renameInput: { id: string; name: string }) =>
      firstValueFrom(this.courseApi.renameGroup(renameInput.id, renameInput.name)),
    onSuccess: () => {
      this.notifications.success('Grupo actualizado.');
      this.queryClient.invalidateQueries({ queryKey: groupsQueryKey(this.source()) });
    },
    onError: () => this.notifications.error('Error al actualizar grupo.'),
  }));

  protected readonly deleteGroup = injectMutation(() => ({
    mutationFn: (groupId: string) => firstValueFrom(this.courseApi.deleteGroup(groupId)),
    onSuccess: () => {
      this.notifications.success('Grupo eliminado.');
      this.queryClient.invalidateQueries({ queryKey: groupsQueryKey(this.source()) });
    },
    onError: () => this.notifications.error('No se pudo eliminar: el grupo aún tiene clases.'),
  }));

  constructor() {
    effect(() => {
      if (this.groupsQuery.isError()) {
        this.notifications.error('Error al cargar grupos.');
      }
    });
  }

  async onCreate(): Promise<void> {
    const name = await this.openNameDialog({ mode: 'create', name: '' });
    const outcome = resolveGroupCreate(name);
    if (outcome.kind === 'skip') {
      return;
    }
    this.createGroup.mutate(outcome.name);
  }

  async onRename(group: ClassGroup): Promise<void> {
    const name = await this.openNameDialog({ mode: 'rename', name: group.name });
    const outcome = resolveGroupRename(group.name, name);
    if (outcome.kind === 'skip') {
      return;
    }
    this.renameGroup.mutate({ id: group.id, name: outcome.name });
  }

  async onDelete(group: ClassGroup): Promise<void> {
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar grupo',
      message: `¿Eliminar el grupo "${group.name}"? Solo es posible si no tiene clases.`,
      destructive: true,
      confirmLabel: 'Eliminar',
    });
    if (!confirmed) {
      return;
    }
    this.deleteGroup.mutate(group.id);
  }

  private openNameDialog(data: GroupNameDialogData): Promise<string | undefined> {
    return this.dialogs.openForm<GroupNameDialog, GroupNameDialogData, string>(
      GroupNameDialog,
      data,
      { width: '420px' },
    );
  }
}
