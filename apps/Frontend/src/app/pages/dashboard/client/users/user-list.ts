import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { QueryClient } from '@tanstack/query-core';
import {
  injectMutation,
  injectQuery,
  keepPreviousData,
} from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '@core/api';
import { AuthService } from '@core/auth';
import { UserListItem } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { EmptyState, Icon, LoadingSkeleton, PageHead, Paginator } from '@shared/components';

const USERNAME_REGEX = /^[a-zA-Z0-9 ]+$/;
const PASSWORD_REGEX = /^[a-zA-Z0-9 !@#$%^&*()_+=?-]+$/;
const MIN_USERNAME = 5;
const MAX_USERNAME = 80;
const MIN_PASSWORD = 5;
const MAX_PASSWORD = 100;

type UserRoleKind = 'student' | 'teacher';

const USERS_QUERY_KEY_ROOT = 'users';

interface RegisterDialogData {
  kind: UserRoleKind;
}

interface RegisterDialogResult {
  username: string;
  password: string;
}

interface RenameDialogData {
  current: string;
}

@Component({
  selector: 'app-register-user-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.kind === 'student' ? 'Nuevo estudiante' : 'Nuevo profesor' }}
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="username" autocomplete="off" />
          @if (form.controls.username.invalid && form.controls.username.touched) {
            <mat-error>5-80 caracteres, letras, números o espacios.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Contraseña</mat-label>
          <input matInput type="password" formControlName="password" autocomplete="new-password" />
          @if (form.controls.password.invalid && form.controls.password.touched) {
            <mat-error>5-100 caracteres permitidos.</mat-error>
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
        (click)="dialogRef.close(form.getRawValue())"
      >
        Registrar
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      min-width: 320px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterUserDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<RegisterUserDialog, RegisterDialogResult>);
  readonly data = inject<RegisterDialogData>(MAT_DIALOG_DATA);

  protected readonly form = this.formBuilder.nonNullable.group({
    username: [
      '',
      [
        Validators.required,
        Validators.minLength(MIN_USERNAME),
        Validators.maxLength(MAX_USERNAME),
        Validators.pattern(USERNAME_REGEX),
      ],
    ],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(MIN_PASSWORD),
        Validators.maxLength(MAX_PASSWORD),
        Validators.pattern(PASSWORD_REGEX),
      ],
    ],
  });
}

@Component({
  selector: 'app-rename-user-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>Renombrar usuario</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Nuevo nombre</mat-label>
          <input matInput formControlName="username" autocomplete="off" />
          @if (form.controls.username.invalid && form.controls.username.touched) {
            <mat-error>5-80 caracteres, letras, números o espacios.</mat-error>
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
        (click)="dialogRef.close(form.getRawValue().username)"
      >
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      min-width: 320px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RenameUserDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<RenameUserDialog, string>);
  readonly data = inject<RenameDialogData>(MAT_DIALOG_DATA);

  protected readonly form = this.formBuilder.nonNullable.group({
    username: [
      this.data.current,
      [
        Validators.required,
        Validators.minLength(MIN_USERNAME),
        Validators.maxLength(MAX_USERNAME),
        Validators.pattern(USERNAME_REGEX),
      ],
    ],
  });
}

@Component({
  selector: 'app-user-list',
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatTooltipModule,
    Icon,
    PageHead,
    Paginator,
    LoadingSkeleton,
    EmptyState,
  ],
  template: `
    <app-page-head [title]="title()" [subtitle]="subtitle()">
      <button actions mat-flat-button color="primary" (click)="onRegister()">
        <app-icon name="user-plus" />
        <span class="btn-label">Nuevo</span>
      </button>
    </app-page-head>

    <mat-card class="list-card">
      <mat-card-content>
        @if (usersQuery.isPending()) {
          <div class="skel-stack">
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
          </div>
        } @else if (users().length === 0) {
          <app-empty-state icon="users" message="No hay usuarios." />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="users()" class="full">
              <ng-container matColumnDef="username">
                <th mat-header-cell *matHeaderCellDef>Nombre</th>
                <td mat-cell *matCellDef="let user">
                  <span class="user-cell">
                    <span class="avatar" [style.background]="avatarColor(user.username)">
                      {{ initials(user.username) }}
                    </span>
                    <span class="truncate">{{ user.username }}</span>
                  </span>
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef class="mat-column-actions">Acciones</th>
                <td mat-cell *matCellDef="let user" class="mat-column-actions">
                  <button mat-icon-button matTooltip="Renombrar" (click)="onRename(user)">
                    <app-icon name="edit" />
                  </button>
                  <button
                    mat-icon-button
                    matTooltip="Eliminar"
                    (click)="onDelete(user)"
                    [disabled]="isSelf(user)"
                    class="danger-btn"
                  >
                    <app-icon name="trash" />
                  </button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>

          <div class="paginator-wrap">
            <app-paginator
              [page]="{ currentIndex: pageIndex(), maxIndex: maxPageIndex() }"
              (pageChange)="changePage($event)"
            />
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
    .danger-btn[disabled] {
      color: var(--dama-text-faint);
    }

    .user-cell {
      display: inline-flex;
      align-items: center;
      gap: 12px;
      max-width: 100%;
    }
    .avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      display: inline-grid;
      place-items: center;
      color: white;
      font-weight: 600;
      font-size: 12px;
      flex-shrink: 0;
      letter-spacing: 0.02em;
    }

    .paginator-wrap {
      display: flex;
      justify-content: center;
      padding: 16px;
      border-top: 1px solid var(--dama-divider);
    }

    .btn-label {
      margin-left: 6px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserList {
  readonly kind = input.required<UserRoleKind>();

  private readonly authApi = inject(AuthApi);
  private readonly authService = inject(AuthService);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  protected readonly pageIndex = signal(0);
  protected readonly columns = ['username', 'actions'];

  protected readonly usersQuery = injectQuery(() => ({
    queryKey: [USERS_QUERY_KEY_ROOT, this.kind(), this.pageIndex()] as const,
    queryFn: () => firstValueFrom(this.fetchPage(this.kind(), this.pageIndex())),
    placeholderData: keepPreviousData,
  }));

  protected readonly users = computed<UserListItem[]>(() => this.usersQuery.data()?.items ?? []);
  protected readonly maxPageIndex = computed(() => this.usersQuery.data()?.maxPageIndex ?? 0);

  protected readonly title = computed(() =>
    this.kind() === 'student' ? 'Estudiantes' : 'Profesores',
  );
  protected readonly subtitle = computed(
    () => `Página ${this.pageIndex() + 1} de ${this.maxPageIndex() + 1}`,
  );

  private readonly registerUser = injectMutation(() => ({
    mutationFn: (input: { roleKind: UserRoleKind; username: string; password: string }) => {
      const registration$ =
        input.roleKind === 'student'
          ? this.authApi.registerStudent({ username: input.username, password: input.password })
          : this.authApi.registerTeacher({ username: input.username, password: input.password });
      return firstValueFrom(registration$);
    },
    onSuccess: () => {
      this.notifications.success('Usuario registrado.');
      this.invalidateCurrentRoleList();
    },
    onError: () => this.notifications.error('Error al registrar.'),
  }));

  private readonly renameUser = injectMutation(() => ({
    mutationFn: (input: { userId: string; username: string }) =>
      firstValueFrom(this.authApi.renameUser(input.userId, { username: input.username })),
    onSuccess: () => {
      this.notifications.success('Usuario renombrado.');
      this.invalidateCurrentRoleList();
    },
    onError: () => this.notifications.error('Error al renombrar.'),
  }));

  private readonly deleteUser = injectMutation(() => ({
    mutationFn: (userId: string) => firstValueFrom(this.authApi.deleteUser(userId)),
    onSuccess: () => {
      this.notifications.success('Usuario eliminado.');
      this.invalidateCurrentRoleList();
    },
    onError: () => this.notifications.error('Error al eliminar.'),
  }));

  constructor() {
    effect(() => {
      if (this.usersQuery.isError()) {
        this.notifications.error('Error al cargar lista.');
      }
    });
  }

  protected isSelf(user: UserListItem): boolean {
    return this.authService.claims()?.userId === user.id;
  }

  protected initials(name: string): string {
    const namePieces = name.trim().split(/\s+/).slice(0, 2);
    return namePieces.map((piece) => piece[0]?.toUpperCase() ?? '').join('') || '?';
  }

  protected avatarColor(name: string): string {
    let hash = 0;
    for (let charIndex = 0; charIndex < name.length; charIndex++) {
      hash = (hash * 31 + name.charCodeAt(charIndex)) | 0;
    }
    const hue = Math.abs(hash) % 360;
    return `hsl(${hue}, 55%, 50%)`;
  }

  changePage(index: number): void {
    this.pageIndex.set(index);
  }

  async onRegister(): Promise<void> {
    const result = await this.dialogs.openForm<
      RegisterUserDialog,
      RegisterDialogData,
      RegisterDialogResult
    >(RegisterUserDialog, { kind: this.kind() }, { width: '440px' });
    if (!result) {
      return;
    }
    this.registerUser.mutate({
      roleKind: this.kind(),
      username: result.username,
      password: result.password,
    });
  }

  async onRename(user: UserListItem): Promise<void> {
    const newName = await this.dialogs.openForm<RenameUserDialog, RenameDialogData, string>(
      RenameUserDialog,
      { current: user.username },
      { width: '440px' },
    );
    if (!newName || newName === user.username) {
      return;
    }
    this.renameUser.mutate({ userId: user.id, username: newName });
  }

  async onDelete(user: UserListItem): Promise<void> {
    if (this.isSelf(user)) {
      return;
    }
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar usuario',
      message: `¿Eliminar a "${user.username}"? El usuario no podrá iniciar sesión.`,
      destructive: true,
      confirmLabel: 'Eliminar',
    });
    if (!confirmed) {
      return;
    }
    this.deleteUser.mutate(user.id);
  }

  private fetchPage(roleKind: UserRoleKind, pageIndex: number) {
    return roleKind === 'student'
      ? this.authApi.listStudents(pageIndex)
      : this.authApi.listTeachers(pageIndex);
  }

  private invalidateCurrentRoleList(): void {
    this.queryClient.invalidateQueries({ queryKey: [USERS_QUERY_KEY_ROOT, this.kind()] });
  }
}
