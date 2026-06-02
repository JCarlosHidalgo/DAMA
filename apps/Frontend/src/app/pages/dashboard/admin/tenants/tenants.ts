import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '@core/api';
import { Tenant } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { EmptyState, Icon, LoadingSkeleton, PageHead } from '@shared/components';
import { NoPasswordManager } from '@shared/directives';

interface TenantDialogData {
  mode: 'create' | 'edit';
  name: string;
}

@Component({
  selector: 'app-tenant-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    NoPasswordManager,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Nuevo tenant' : 'Editar tenant' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" autocomplete="off" />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
          @if (form.controls.name.hasError('maxlength')) {
            <mat-error>Máximo 200 caracteres</mat-error>
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
export class TenantDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<TenantDialog, string>);
  readonly data = inject<TenantDialogData>(MAT_DIALOG_DATA);

  readonly form = this.formBuilder.nonNullable.group({
    name: [this.data.name, [Validators.required, Validators.maxLength(200)]],
  });
}

@Component({
  selector: 'app-tenants',
  imports: [MatCardModule, MatButtonModule, Icon, PageHead, EmptyState, LoadingSkeleton],
  template: `
    <app-page-head title="Tenants" subtitle="Gestiona las escuelas registradas.">
      <button mat-flat-button color="primary" (click)="onCreate()">
        <app-icon name="plus" />
        Crear tenant
      </button>
    </app-page-head>

    @if (isLoading()) {
      <app-loading-skeleton />
    } @else if (isError()) {
      <app-empty-state icon="warning" message="No se pudieron cargar los tenants." />
    } @else if (tenants().length === 0) {
      <app-empty-state icon="building" message="Aún no hay tenants registrados." />
    } @else {
      <div class="tenant-grid">
        @for (tenant of tenants(); track tenant.id) {
          <mat-card
            class="tenant-card"
            appearance="outlined"
            tabindex="0"
            (click)="onEdit(tenant)"
            (keyup.enter)="onEdit(tenant)"
          >
            <mat-card-content class="tenant-card__content">
              <app-icon class="tenant-card__icon" name="building" />
              <h2 class="tenant-card__name">{{ tenant.name }}</h2>
              <p class="tenant-card__tz">{{ tenant.timezone }}</p>
            </mat-card-content>
          </mat-card>
        }
      </div>
    }
  `,
  styles: `
    .tenant-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 1rem;
    }

    .tenant-card {
      cursor: pointer;
      transition: box-shadow 150ms ease;
    }

    .tenant-card:hover,
    .tenant-card:focus-visible {
      box-shadow: 0 4px 16px rgb(0 0 0 / 0.12);
    }

    .tenant-card__content {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .tenant-card__icon {
      margin-bottom: 0.5rem;
      font-size: 1.75rem;
      color: var(--mat-sys-primary, #3f51b5);
    }

    .tenant-card__name {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 600;
    }

    .tenant-card__tz {
      margin: 0;
      color: rgb(0 0 0 / 0.6);
      font-size: 0.85rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Tenants {
  private readonly authApi = inject(AuthApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly tenants = signal<Tenant[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly isError = signal(false);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    this.isLoading.set(true);
    this.isError.set(false);
    try {
      this.tenants.set(await firstValueFrom(this.authApi.listTenants()));
    } catch {
      this.isError.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }

  async onCreate(): Promise<void> {
    const name = await this.dialogs.openForm<TenantDialog, TenantDialogData, string>(
      TenantDialog,
      { mode: 'create', name: '' },
      { width: '420px' },
    );
    if (!name) {
      return;
    }
    try {
      await firstValueFrom(this.authApi.createTenant({ name }));
      this.notifications.success('Tenant creado.');
      await this.load();
    } catch {
      this.notifications.error('No se pudo crear el tenant.');
    }
  }

  async onEdit(tenant: Tenant): Promise<void> {
    const name = await this.dialogs.openForm<TenantDialog, TenantDialogData, string>(
      TenantDialog,
      { mode: 'edit', name: tenant.name },
      { width: '420px' },
    );
    if (!name || name === tenant.name) {
      return;
    }
    try {
      await firstValueFrom(this.authApi.renameTenant(tenant.id, { name }));
      this.notifications.success('Tenant actualizado.');
      await this.load();
    } catch {
      this.notifications.error('No se pudo actualizar el tenant.');
    }
  }
}
