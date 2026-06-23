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
import { EmptyState, Icon, LoadingSkeleton, PageHead } from '@shared/design/components';
import { NoPasswordManager } from '@shared/directives';

import { resolveTenantCreate, resolveTenantEdit } from './tenants.logic';
import { tenantsStyles } from './tenants.variants';

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
      <div [class]="styles.grid()">
        @for (tenant of tenants(); track tenant.id) {
          <mat-card
            [class]="styles.card()"
            appearance="outlined"
            tabindex="0"
            (click)="onEdit(tenant)"
            (keyup.enter)="onEdit(tenant)"
          >
            <mat-card-content [class]="styles.cardContent()">
              <app-icon [class]="styles.cardIcon()" name="building" />
              <h2 [class]="styles.cardName()">{{ tenant.name }}</h2>
              <p [class]="styles.cardTz()">{{ tenant.timezone }}</p>
            </mat-card-content>
          </mat-card>
        }
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Tenants {
  private readonly authApi = inject(AuthApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly styles = tenantsStyles();
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
    const outcome = resolveTenantCreate(name);
    if (outcome.kind === 'skip') {
      return;
    }
    try {
      await firstValueFrom(this.authApi.createTenant({ name: outcome.name }));
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
    const outcome = resolveTenantEdit(tenant.name, name);
    if (outcome.kind === 'skip') {
      return;
    }
    try {
      await firstValueFrom(this.authApi.renameTenant(tenant.id, { name: outcome.name }));
      this.notifications.success('Tenant actualizado.');
      await this.load();
    } catch {
      this.notifications.error('No se pudo actualizar el tenant.');
    }
  }
}
