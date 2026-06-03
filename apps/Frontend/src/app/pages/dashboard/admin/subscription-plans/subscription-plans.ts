import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';

import { PaymentApi } from '@core/api';
import { SubscriptionDurationUnit, SubscriptionPlan } from '@core/models';
import { NotificationService } from '@core/services';
import { LoadingSkeleton, PageHead } from '@shared/components';

import { adminSubscriptionPlansStyles } from './subscription-plans.variants';

interface PlanRow {
  level: number;
  form: FormGroup<{
    price: FormControl<number>;
    durationAmount: FormControl<number>;
    durationUnit: FormControl<string>;
  }>;
}

const DURATION_UNITS: SubscriptionDurationUnit[] = ['Day', 'Week', 'Month'];

@Component({
  selector: 'app-admin-subscription-plans',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    PageHead,
    LoadingSkeleton,
  ],
  template: `
    <app-page-head
      title="Planes de suscripción"
      subtitle="Define el precio y la duración de cada nivel de la pirámide."
    />

    @if (loading()) {
      <div [class]="styles.grid()">
        <app-loading-skeleton [height]="200" />
        <app-loading-skeleton [height]="200" />
        <app-loading-skeleton [height]="200" />
      </div>
    } @else {
      <div [class]="styles.grid()">
        @for (row of rows(); track row.level) {
          <mat-card class="plan-card">
            <mat-card-content>
              <h2 class="t-h2">Nivel {{ row.level }}</h2>
              <form [formGroup]="row.form" [class]="styles.form()">
                <mat-form-field appearance="outline" [class]="styles.field()">
                  <mat-label>Precio</mat-label>
                  <input matInput type="number" min="1" formControlName="price" />
                </mat-form-field>

                <mat-form-field appearance="outline" [class]="styles.field()">
                  <mat-label>Duración</mat-label>
                  <input matInput type="number" min="1" formControlName="durationAmount" />
                </mat-form-field>

                <mat-form-field appearance="outline" [class]="styles.field()">
                  <mat-label>Unidad</mat-label>
                  <mat-select formControlName="durationUnit">
                    @for (unit of durationUnits; track unit) {
                      <mat-option [value]="unit">{{ unitLabel(unit) }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </form>
            </mat-card-content>
            <mat-card-actions align="end">
              <button
                mat-flat-button
                color="primary"
                [disabled]="row.form.invalid || saving() === row.level"
                (click)="onSave(row)"
              >
                Guardar
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    }
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSubscriptionPlans {
  private readonly paymentApi = inject(PaymentApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly notifications = inject(NotificationService);

  protected readonly styles = adminSubscriptionPlansStyles();
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly rows = signal<PlanRow[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal<number | null>(null);

  constructor() {
    this.load();
  }

  protected unitLabel(unit: SubscriptionDurationUnit): string {
    return { Day: 'Días', Week: 'Semanas', Month: 'Meses' }[unit];
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const plans = await firstValueFrom(this.paymentApi.listSubscriptionPlans());
      const sorted = [...(plans ?? [])].sort((first, second) => first.level - second.level);
      this.rows.set(sorted.map((plan) => this.toRow(plan)));
    } catch {
      this.notifications.error('Error al cargar los planes.');
    } finally {
      this.loading.set(false);
    }
  }

  private toRow(plan: SubscriptionPlan): PlanRow {
    return {
      level: plan.level,
      form: this.formBuilder.nonNullable.group({
        price: [plan.price, [Validators.required, Validators.min(1)]],
        durationAmount: [plan.durationAmount, [Validators.required, Validators.min(1)]],
        durationUnit: [plan.durationUnit as string, Validators.required],
      }),
    };
  }

  async onSave(row: PlanRow): Promise<void> {
    if (row.form.invalid) {
      return;
    }
    this.saving.set(row.level);
    try {
      const value = row.form.getRawValue();
      await firstValueFrom(
        this.paymentApi.updateSubscriptionPlan(row.level, {
          price: Number(value.price),
          durationAmount: Number(value.durationAmount),
          durationUnit: value.durationUnit as SubscriptionDurationUnit,
        }),
      );
      this.notifications.success(`Plan nivel ${row.level} actualizado.`);
    } catch {
      this.notifications.error('Error al guardar el plan. Revisa los límites (1 día a 1 año).');
    } finally {
      this.saving.set(null);
    }
  }
}
