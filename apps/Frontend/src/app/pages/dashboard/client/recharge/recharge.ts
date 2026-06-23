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
import { Icon, PageHead } from '@shared/design/components';
import { NoPasswordManager } from '@shared/directives';

import {
  studentRechargeConfirmMessage,
  studentRechargeSuccessMessage,
  tenantRechargeConfirmMessage,
  tenantRechargeSuccessMessage,
} from './recharge.logic';
import { rechargeStyles } from './recharge.variants';

@Component({
  selector: 'app-recharge',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    NoPasswordManager,
    Icon,
    PageHead,
  ],
  template: `
    <app-page-head title="Recargas" subtitle="Asigna clases a estudiantes." />

    <div [class]="styles.grid()">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Recarga por estudiante</mat-card-title>
          <mat-card-subtitle>Buscar por nombre exacto</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="studentForm" (ngSubmit)="onSubmitStudent()" [class]="styles.form()">
            <mat-form-field appearance="outline" [class]="styles.field()">
              <mat-label>Nombre del estudiante</mat-label>
              <input matInput formControlName="name" autocomplete="off" />
              @if (studentForm.controls.name.hasError('required')) {
                <mat-error>Requerido</mat-error>
              }
              @if (studentForm.controls.name.hasError('minlength')) {
                <mat-error>Mínimo 5 caracteres</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" [class]="styles.field()">
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
              [class]="styles.submit()"
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
          <div [class]="styles.callout()">
            <app-icon name="warning" [class]="styles.calloutIcon()" />
            <p [class]="styles.calloutText()">
              Solo se recargarán estudiantes que ya tengan al menos 1 clase de saldo. No se crearán
              filas nuevas.
            </p>
          </div>

          <form [formGroup]="tenantForm" (ngSubmit)="onSubmitTenant()" [class]="styles.form()">
            <mat-form-field appearance="outline" [class]="styles.field()">
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
              [class]="styles.submit()"
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
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Recharge {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly styles = rechargeStyles();
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
        message: studentRechargeConfirmMessage(quantity, student.username),
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
      this.notifications.success(studentRechargeSuccessMessage(quantity, student.username));
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
      message: tenantRechargeConfirmMessage(quantity),
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
      this.notifications.success(tenantRechargeSuccessMessage(result.affected));
      this.tenantForm.reset({ quantity: 1 });
    } catch {
      this.notifications.error('Error al recargar.');
    } finally {
      this.tenantBusy.set(false);
    }
  }
}
