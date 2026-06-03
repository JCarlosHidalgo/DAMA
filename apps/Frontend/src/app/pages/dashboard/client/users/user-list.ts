import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
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
import {
  EmptyState,
  ErrorState,
  Icon,
  LoadingSkeleton,
  PageHead,
  Paginator,
  ResponsiveTable,
  type ResponsiveTableColumn,
  TableCell,
} from '@shared/components';
import { NoPasswordManager } from '@shared/directives';

import { userDialogStyles, userListStyles } from './user-list.variants';

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
    NoPasswordManager,
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.kind === 'student' ? 'Nuevo estudiante' : 'Nuevo profesor' }}
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" [class]="styles.form()">
        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="username" autocomplete="off" />
          @if (form.controls.username.invalid && form.controls.username.touched) {
            <mat-error>5-80 caracteres, letras, números o espacios.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterUserDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<RegisterUserDialog, RegisterDialogResult>);
  readonly data = inject<RegisterDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = userDialogStyles();

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
    NoPasswordManager,
  ],
  template: `
    <h2 mat-dialog-title>Renombrar usuario</h2>
    <mat-dialog-content>
      <form [formGroup]="form" [class]="styles.form()">
        <mat-form-field appearance="outline" [class]="styles.field()">
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RenameUserDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<RenameUserDialog, string>);
  readonly data = inject<RenameDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = userDialogStyles();

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
    MatButtonModule,
    MatTooltipModule,
    Icon,
    PageHead,
    Paginator,
    LoadingSkeleton,
    EmptyState,
    ErrorState,
    ResponsiveTable,
    TableCell,
  ],
  template: `
    <app-page-head [title]="title()" [subtitle]="subtitle()">
      <button actions mat-flat-button color="primary" (click)="onRegister()">
        <app-icon name="user-plus" />
        <span [class]="styles.buttonLabel()">Nuevo</span>
      </button>
    </app-page-head>

    <mat-card [class]="styles.listCard()">
      <mat-card-content [class]="styles.cardContent()">
        @if (usersQuery.isPending()) {
          <div [class]="styles.skelStack()">
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
          </div>
        } @else if (usersQuery.isError()) {
          <app-error-state message="No se pudo cargar la lista.">
            <button action mat-stroked-button (click)="usersQuery.refetch()">Reintentar</button>
          </app-error-state>
        } @else if (users().length === 0) {
          <app-empty-state icon="users" message="No hay usuarios." />
        } @else {
          <app-responsive-table [columns]="tableColumns" [rows]="users()">
            <ng-template appTableCell="username" let-user>
              <span [class]="styles.userCell()">
                <span [class]="styles.avatar()" [style.background]="avatarColor(user.username)">
                  {{ initials(user.username) }}
                </span>
                <span class="truncate">{{ user.username }}</span>
              </span>
            </ng-template>
            <ng-template appTableCell="actions" let-user>
              <button mat-icon-button matTooltip="Renombrar" (click)="onRename(user)">
                <app-icon name="edit" />
              </button>
              <button
                mat-icon-button
                matTooltip="Eliminar"
                (click)="onDelete(user)"
                [disabled]="isSelf(user)"
                [class]="styles.dangerButton()"
              >
                <app-icon name="trash" />
              </button>
            </ng-template>
          </app-responsive-table>

          <div [class]="styles.paginatorWrap()">
            <app-paginator
              [page]="{ currentIndex: pageIndex(), maxIndex: maxPageIndex() }"
              (pageChange)="changePage($event)"
            />
          </div>
        }
      </mat-card-content>
    </mat-card>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserList {
  readonly kind = input.required<UserRoleKind>();

  private readonly authApi = inject(AuthApi);
  private readonly authService = inject(AuthService);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  protected readonly styles = userListStyles();
  protected readonly pageIndex = signal(0);
  protected readonly tableColumns: ResponsiveTableColumn[] = [
    { key: 'username', header: 'Nombre' },
    { key: 'actions', header: 'Acciones', mobileLayout: 'block' },
  ];

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
    onError: (error) => this.notifications.errorFrom(error, 'Error al registrar.'),
  }));

  private readonly renameUser = injectMutation(() => ({
    mutationFn: (input: { userId: string; username: string }) =>
      firstValueFrom(this.authApi.renameUser(input.userId, { username: input.username })),
    onSuccess: () => {
      this.notifications.success('Usuario renombrado.');
      this.invalidateCurrentRoleList();
    },
    onError: (error) => this.notifications.errorFrom(error, 'Error al renombrar.'),
  }));

  private readonly deleteUser = injectMutation(() => ({
    mutationFn: (userId: string) => firstValueFrom(this.authApi.deleteUser(userId)),
    onSuccess: () => {
      this.notifications.success('Usuario eliminado.');
      this.invalidateCurrentRoleList();
    },
    onError: (error) => this.notifications.errorFrom(error, 'Error al eliminar.'),
  }));

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
