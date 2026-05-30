import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { AttendanceApi, AuthApi } from '@core/api';
import { UserListItem } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { Icon, PageHead } from '@shared/components';

@Component({
  selector: 'app-recharge',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    Icon,
    PageHead,
  ],
  template: `
    <app-page-head title="Recargas" subtitle="Asigna clases a estudiantes." />

    <div class="grid">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Recarga por estudiante</mat-card-title>
          <mat-card-subtitle>Buscar por nombre exacto</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="studentForm" (ngSubmit)="onSubmitStudent()">
            <mat-form-field appearance="outline">
              <mat-label>Nombre del estudiante</mat-label>
              <input matInput formControlName="name" autocomplete="off" />
              @if (studentForm.controls.name.hasError('required')) {
                <mat-error>Requerido</mat-error>
              }
              @if (studentForm.controls.name.hasError('minlength')) {
                <mat-error>Mínimo 5 caracteres</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Cantidad de clases</mat-label>
              <input matInput type="number" formControlName="quantity" min="1" max="49" />
              @if (studentForm.controls.quantity.invalid && studentForm.controls.quantity.touched) {
                <mat-error>Entre 1 y 49</mat-error>
              }
            </mat-form-field>

            <button
              mat-flat-button
              color="primary"
              type="submit"
              [disabled]="studentForm.invalid || studentBusy()"
            >
              @if (studentBusy()) {
                <mat-spinner diameter="20" />
              } @else {
                Recargar
              }
            </button>
          </form>
        </mat-card-content>
      </mat-card>

      <mat-card>
        <mat-card-header>
          <mat-card-title>Recarga masiva</mat-card-title>
          <mat-card-subtitle>Aplica a todos los estudiantes con saldo previo</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <div class="callout warn">
            <app-icon name="warning" />
            <p>
              Solo se recargarán estudiantes que ya tengan al menos 1 clase de saldo. No se crearán
              filas nuevas.
            </p>
          </div>

          <form [formGroup]="tenantForm" (ngSubmit)="onSubmitTenant()">
            <mat-form-field appearance="outline">
              <mat-label>Cantidad de clases</mat-label>
              <input matInput type="number" formControlName="quantity" min="1" max="49" />
              @if (tenantForm.controls.quantity.invalid && tenantForm.controls.quantity.touched) {
                <mat-error>Entre 1 y 49</mat-error>
              }
            </mat-form-field>

            <button
              mat-flat-button
              color="primary"
              type="submit"
              [disabled]="tenantForm.invalid || tenantBusy()"
            >
              @if (tenantBusy()) {
                <mat-spinner diameter="20" />
              } @else {
                Recargar a todos
              }
            </button>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: var(--dama-space-4);
    }
    form {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    mat-form-field {
      width: 100%;
    }
    button[mat-flat-button] {
      align-self: flex-end;
      min-width: 140px;
      height: 44px;
    }

    .callout {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 12px 14px;
      border-radius: var(--dama-radius-sm);
      margin-bottom: 14px;
      font-size: 13px;
      line-height: 1.4;

      &.warn {
        background: var(--dama-warning-soft);
        color: color-mix(in oklab, var(--dama-warning) 80%, var(--dama-text));
        border: 1px solid color-mix(in oklab, var(--dama-warning) 30%, transparent);
        app-icon {
          color: var(--dama-warning);
          font-size: 16px;
          flex-shrink: 0;
          margin-top: 1px;
        }
      }
      p {
        margin: 0;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Recharge {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly studentBusy = signal(false);
  protected readonly tenantBusy = signal(false);

  protected readonly studentForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(5)]],
    quantity: [1, [Validators.required, Validators.min(1), Validators.max(49)]],
  });

  protected readonly tenantForm = this.formBuilder.nonNullable.group({
    quantity: [1, [Validators.required, Validators.min(1), Validators.max(49)]],
  });

  async onSubmitStudent(): Promise<void> {
    if (this.studentForm.invalid || this.studentBusy()) {
      return;
    }
    const { name, quantity } = this.studentForm.getRawValue();
    this.studentBusy.set(true);
    try {
      let student: UserListItem;
      try {
        student = await firstValueFrom(this.authApi.searchStudentByName(name));
      } catch {
        this.notifications.error('No se encontró un estudiante con ese nombre.');
        return;
      }

      const confirmed = await this.dialogs.confirm({
        title: 'Confirmar recarga',
        message: `Agregar ${quantity} clase(s) a ${student.username}?`,
      });
      if (!confirmed) {
        return;
      }

      await firstValueFrom(
        this.attendanceApi.clientIncrementStudent(student.id, {
          requestId: crypto.randomUUID(),
          quantity,
          studentName: student.username,
        }),
      );
      this.notifications.success(`Recargadas ${quantity} clase(s) a ${student.username}.`);
      this.studentForm.reset({ name: '', quantity: 1 });
    } catch {
      this.notifications.error('Error al recargar.');
    } finally {
      this.studentBusy.set(false);
    }
  }

  async onSubmitTenant(): Promise<void> {
    if (this.tenantForm.invalid || this.tenantBusy()) {
      return;
    }
    const { quantity } = this.tenantForm.getRawValue();
    const confirmed = await this.dialogs.confirm({
      title: 'Confirmar recarga masiva',
      message: `Agregar ${quantity} clase(s) a TODOS los estudiantes con saldo previo. ¿Continuar?`,
      destructive: true,
      confirmLabel: 'Recargar a todos',
    });
    if (!confirmed) {
      return;
    }
    this.tenantBusy.set(true);
    try {
      const result = await firstValueFrom(
        this.attendanceApi.clientIncrementTenant({ requestId: crypto.randomUUID(), quantity }),
      );
      this.notifications.success(`Actualizados ${result.affected} estudiantes.`);
      this.tenantForm.reset({ quantity: 1 });
    } catch {
      this.notifications.error('Error al recargar.');
    } finally {
      this.tenantBusy.set(false);
    }
  }
}
