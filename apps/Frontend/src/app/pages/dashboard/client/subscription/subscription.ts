import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import {
  MatDialog,
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { PaymentApi } from '@core/api';
import { AuthService } from '@core/auth';
import { QrDebtStatus, SubscriptionPlan } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { pollQrDebtUntilSettled } from '@core/utils';
import { EmptyState, LoadingSkeleton, PageHead, Tag } from '@shared/components';
import { MoneyPipe } from '@shared/pipes';

import {
  clientSubscriptionStyles,
  subscriptionPayDialogStyles,
  subscriptionQrImageDialogStyles,
} from './subscription.variants';

const LEVEL_LABELS: Record<number, string> = {
  1: 'Base — cursos y clases',
  2: 'Intermedio — + estudiantes, profesores y asistencia',
  3: 'Completo — + gestión de pagos',
};

const DURATION_UNIT_LABELS: Record<string, string> = {
  Day: 'día(s)',
  Week: 'semana(s)',
  Month: 'mes(es)',
};

function describePlanDuration(plan: SubscriptionPlan): string {
  return `${plan.durationAmount} ${DURATION_UNIT_LABELS[plan.durationUnit] ?? plan.durationUnit}`;
}

interface SubscriptionQrImageDialogData {
  debtId: string;
  qrUrl: string;
}

@Component({
  selector: 'app-subscription-qr-image-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>QR de suscripción</h2>
    <mat-dialog-content [class]="styles.content()">
      <img
        [class]="styles.image()"
        [src]="data.qrUrl"
        [alt]="'QR de suscripción ' + data.debtId"
        referrerpolicy="no-referrer"
      />
      <p [class]="styles.hint()">Escanea el QR desde tu app bancaria para completar el pago.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <a
        mat-button
        [href]="data.qrUrl"
        [download]="'suscripcion-' + data.debtId + '.png'"
        target="_blank"
        rel="noopener noreferrer"
      >
        Descargar
      </a>
      <button mat-flat-button color="primary" (click)="dialogRef.close()">Cerrar</button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscriptionQrImageDialog {
  readonly dialogRef = inject(MatDialogRef<SubscriptionQrImageDialog>);
  readonly data = inject<SubscriptionQrImageDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = subscriptionQrImageDialogStyles();
}

interface SubscriptionPayDialogData {
  plans: SubscriptionPlan[];
}

interface SubscriptionPayDialogResult {
  level: number;
}

@Component({
  selector: 'app-subscription-pay-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MoneyPipe,
  ],
  template: `
    <h2 mat-dialog-title>Pagar suscripción</h2>
    <mat-dialog-content>
      <form [formGroup]="form" [class]="styles.form()">
        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Nivel de suscripción</mat-label>
          <mat-select formControlName="level">
            @for (plan of data.plans; track plan.level) {
              <mat-option [value]="plan.level">
                Nivel {{ plan.level }} · {{ plan.price | money }} / {{ planDuration(plan) }}
              </mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Método de pago</mat-label>
          <mat-select formControlName="method">
            <mat-option value="QR">QR (Todotix)</mat-option>
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="submit()">
        Registrar deuda
      </button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscriptionPayDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<SubscriptionPayDialog, SubscriptionPayDialogResult>);
  readonly data = inject<SubscriptionPayDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = subscriptionPayDialogStyles();

  protected readonly form = this.formBuilder.nonNullable.group({
    level: [this.data.plans[0]?.level ?? 1, Validators.required],
    method: ['QR', Validators.required],
  });

  protected planDuration(plan: SubscriptionPlan): string {
    return describePlanDuration(plan);
  }

  submit(): void {
    this.dialogRef.close({ level: this.form.getRawValue().level });
  }
}

@Component({
  selector: 'app-client-subscription',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    PageHead,
    LoadingSkeleton,
    EmptyState,
    Tag,
    MoneyPipe,
  ],
  template: `
    <app-page-head title="Suscripción" subtitle="Gestiona el plan de tu escuela en DAMA." />

    <mat-card [class]="styles.statusCard()">
      <mat-card-content>
        @if (effectiveIndex() > 0) {
          <div [class]="styles.statusHead()">
            <app-tag variant="primary" [dot]="true">Nivel {{ effectiveIndex() }}</app-tag>
            <span class="t-body">Vence el {{ expiresLabel() }}</span>
          </div>
        } @else {
          <div [class]="styles.statusHead()">
            <app-tag variant="neutral" [dot]="true">Sin suscripción vigente</app-tag>
            <span class="t-body">Compra un plan para habilitar los servicios de tu escuela.</span>
          </div>
        }
        <div class="status-actions">
          <button
            mat-flat-button
            color="primary"
            [disabled]="loading() || plans().length === 0 || paying()"
            (click)="onPay()"
          >
            @if (paying()) {
              <mat-spinner diameter="20" />
            } @else {
              <span>Pagar Suscripción</span>
            }
          </button>
        </div>
      </mat-card-content>
    </mat-card>

    @if (loading()) {
      <div [class]="styles.grid()">
        <app-loading-skeleton [height]="160" />
        <app-loading-skeleton [height]="160" />
        <app-loading-skeleton [height]="160" />
      </div>
    } @else if (plans().length === 0) {
      <app-empty-state icon="receipt" message="No hay planes de suscripción disponibles." />
    } @else {
      <div [class]="styles.grid()">
        @for (plan of plans(); track plan.level) {
          <mat-card class="plan-card">
            <mat-card-content>
              <div [class]="styles.planHead()">
                <span class="t-h2">Nivel {{ plan.level }}</span>
                <app-tag variant="primary" [dot]="true">{{ planDuration(plan) }}</app-tag>
              </div>
              <p [class]="styles.planDesc()">{{ levelLabel(plan.level) }}</p>
              <div [class]="styles.price()">{{ plan.price | money }}</div>
            </mat-card-content>
          </mat-card>
        }
      </div>
    }
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientSubscription {
  private readonly paymentApi = inject(PaymentApi);
  private readonly authService = inject(AuthService);
  private readonly matDialog = inject(MatDialog);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly styles = clientSubscriptionStyles();
  protected readonly plans = signal<SubscriptionPlan[]>([]);
  protected readonly loading = signal(true);
  protected readonly paying = signal(false);

  protected readonly effectiveIndex = computed(() => this.authService.effectiveSubscriptionIndex());
  protected readonly expiresLabel = computed(() => {
    const expiresAt = this.authService.claims()?.subscriptionExpiresAt ?? 0;
    if (expiresAt <= 0) {
      return '—';
    }
    return new Date(expiresAt * 1000).toLocaleDateString('es', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  });

  constructor() {
    this.load();
  }

  protected levelLabel(level: number): string {
    return LEVEL_LABELS[level] ?? `Nivel ${level}`;
  }

  protected planDuration(plan: SubscriptionPlan): string {
    return describePlanDuration(plan);
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const plans = await firstValueFrom(this.paymentApi.listSubscriptionPlans());
      this.plans.set([...(plans ?? [])].sort((first, second) => first.level - second.level));
    } catch {
      this.notifications.error('Error al cargar los planes de suscripción.');
    } finally {
      this.loading.set(false);
    }
  }

  async onPay(): Promise<void> {
    const result = await this.dialogs.openForm<
      SubscriptionPayDialog,
      SubscriptionPayDialogData,
      SubscriptionPayDialogResult
    >(SubscriptionPayDialog, { plans: this.plans() }, { width: '460px' });
    if (!result) {
      return;
    }

    const confirmed = await this.dialogs.confirm({
      title: 'Confirmar pago de suscripción',
      message: `¿Registrar la deuda para el nivel ${result.level}?`,
      confirmLabel: 'Registrar deuda',
    });
    if (!confirmed) {
      return;
    }

    this.paying.set(true);
    try {
      const queued = await firstValueFrom(this.paymentApi.createSubscriptionQr(result.level));
      if (queued.alreadyGenerated) {
        this.notifications.info('Ya tenías una deuda pendiente; aquí está tu QR.');
      }
      const finalStatus = await this.pollUntilSettled(queued.identificadorDeuda);

      if (finalStatus.status === 'Ready' && finalStatus.qrSimpleUrl) {
        this.openQrDialog(finalStatus.identificadorDeuda, finalStatus.qrSimpleUrl);
      } else if (finalStatus.status === 'Failed') {
        this.notifications.error(
          `Error al generar QR: ${finalStatus.error ?? 'reintente más tarde.'}`,
        );
      } else {
        this.notifications.info('Generación en curso. Vuelve a intentar en unos segundos.');
      }
    } catch {
      this.notifications.error('Error al registrar la deuda. Intenta de nuevo.');
    } finally {
      this.paying.set(false);
    }
  }

  private pollUntilSettled(debtId: string): Promise<QrDebtStatus> {
    return pollQrDebtUntilSettled(debtId, (id) => this.paymentApi.getSubscriptionQrStatus(id));
  }

  private openQrDialog(debtId: string, qrUrl: string): void {
    this.matDialog.open<SubscriptionQrImageDialog, SubscriptionQrImageDialogData>(
      SubscriptionQrImageDialog,
      { data: { debtId, qrUrl }, width: '400px' },
    );
  }
}
