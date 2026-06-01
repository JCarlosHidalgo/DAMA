import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
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
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { firstValueFrom } from 'rxjs';

import { PaymentApi } from '@core/api';
import { DebtTemplate, QrDebtStatus } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { pollQrDebtUntilSettled } from '@core/utils';
import { EmptyState, Icon, LoadingSkeleton, PageHead, Tag } from '@shared/components';
import { MoneyPipe } from '@shared/pipes';

type PaymentMethod = 'qr';

interface PayDialogData {
  template: DebtTemplate;
}

interface PayDialogResult {
  method: PaymentMethod;
  email: string | null;
}

interface QrImageDialogData {
  debtId: string;
  qrUrl: string;
}

@Component({
  selector: 'app-qr-image-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>QR generado</h2>
    <mat-dialog-content class="qr-content">
      <img class="qr-image" [src]="data.qrUrl" [alt]="'QR de pago ' + data.debtId" loading="lazy" />
      <p class="hint t-small">Escanea el QR desde tu app bancaria para completar el pago.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <a
        mat-button
        [href]="data.qrUrl"
        [download]="'qr-' + data.debtId + '.png'"
        target="_blank"
        rel="noopener noreferrer"
      >
        Descargar
      </a>
      <button mat-flat-button color="primary" (click)="dialogRef.close()">Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: `
    .qr-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      min-width: 280px;
    }
    .qr-image {
      width: 100%;
      max-width: 320px;
      height: auto;
      display: block;
    }
    .hint {
      color: var(--dama-text-muted);
      text-align: center;
      margin: 0;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QrImageDialog {
  readonly dialogRef = inject(MatDialogRef<QrImageDialog>);
  readonly data = inject<QrImageDialogData>(MAT_DIALOG_DATA);
}

@Component({
  selector: 'app-no-payment-credentials-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Pagos no disponibles</h2>
    <mat-dialog-content>
      <p class="message t-body">
        Tu escuela no tiene credenciales de pago configuradas, comunícate con los administradores.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" (click)="dialogRef.close()">Entendido</button>
    </mat-dialog-actions>
  `,
  styles: `
    .message {
      margin: 0;
      min-width: 280px;
      color: var(--dama-text);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NoPaymentCredentialsDialog {
  readonly dialogRef = inject(MatDialogRef<NoPaymentCredentialsDialog>);
}

@Component({
  selector: 'app-pay-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatRadioModule,
    MatButtonModule,
    MoneyPipe,
  ],
  template: `
    <h2 mat-dialog-title>Pagar</h2>
    <mat-dialog-content>
      <div class="info">
        <div>
          <strong>{{ data.template.description }}</strong>
        </div>
        <div>{{ data.template.classQuantity }} clase(s) · {{ data.template.cost | money }}</div>
      </div>

      <form [formGroup]="form" class="form">
        <mat-radio-group formControlName="method" class="methods">
          <mat-radio-button value="qr">QR (Todotix)</mat-radio-button>
        </mat-radio-group>

        <mat-form-field appearance="outline">
          <mat-label>Email (opcional)</mat-label>
          <input matInput type="email" formControlName="email" autocomplete="email" />
          @if (form.controls.email.hasError('email')) {
            <mat-error>Formato de email inválido</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="submit()">
        Generar QR
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .info {
      margin-bottom: 12px;
    }
    .form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      min-width: 320px;
    }
    .methods {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PayDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<PayDialog, PayDialogResult>);
  readonly data = inject<PayDialogData>(MAT_DIALOG_DATA);

  protected readonly form = this.formBuilder.nonNullable.group({
    method: ['qr' as PaymentMethod, Validators.required],
    email: ['', Validators.email],
  });

  submit(): void {
    const formValue = this.form.getRawValue();
    this.dialogRef.close({
      method: formValue.method,
      email: formValue.email.trim() === '' ? null : formValue.email.trim(),
    });
  }
}

@Component({
  selector: 'app-pay-classes',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    Icon,
    PageHead,
    LoadingSkeleton,
    EmptyState,
    Tag,
    MoneyPipe,
  ],
  template: `
    <app-page-head title="Comprar clases" subtitle="Elige un paquete y paga con QR." />

    @if (!loading() && !paymentConfigured()) {
      <div class="no-credentials-banner">
        <app-icon name="warning" />
        <span
          >Tu escuela no tiene credenciales de pago configuradas, comunícate con los
          administradores.</span
        >
      </div>
    }

    @if (loading()) {
      <div class="grid">
        <app-loading-skeleton [height]="180" />
        <app-loading-skeleton [height]="180" />
        <app-loading-skeleton [height]="180" />
      </div>
    } @else if (templates().length === 0) {
      <app-empty-state icon="receipt" message="No hay paquetes disponibles." />
    } @else {
      <div class="grid">
        @for (template of templates(); track template.id) {
          <mat-card class="pack-card">
            <mat-card-content>
              <div class="pack-head">
                <span class="t-h2">{{ template.description }}</span>
                <app-tag variant="primary" [dot]="true"
                  >{{ template.classQuantity }} clase(s)</app-tag
                >
              </div>
              <div class="t-num-lg price tabular-nums">{{ template.cost | money }}</div>
            </mat-card-content>
            <mat-card-actions align="end">
              <button
                mat-flat-button
                color="primary"
                [disabled]="!paymentConfigured() || paying() === template.id"
                (click)="onPay(template)"
              >
                @if (paying() === template.id) {
                  <mat-spinner diameter="20" />
                } @else {
                  <app-icon name="qr" /><span class="btn-label">Pagar</span>
                }
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .no-credentials-banner {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 16px;
      margin-bottom: var(--dama-space-4);
      border-radius: var(--dama-radius);
      background: color-mix(in oklab, var(--dama-warning) 14%, transparent);
      color: var(--dama-text-strong);
      border: 1px solid color-mix(in oklab, var(--dama-warning) 40%, transparent);
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: var(--dama-space-4);
    }
    .pack-card {
      display: flex;
      flex-direction: column;
    }
    .pack-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      margin-bottom: 12px;
    }
    .price {
      color: var(--dama-text-strong);
    }
    .btn-label {
      margin-left: 6px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PayClasses {
  private readonly paymentApi = inject(PaymentApi);
  private readonly matDialog = inject(MatDialog);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly templates = signal<DebtTemplate[]>([]);
  protected readonly loading = signal(true);
  protected readonly paying = signal<string | null>(null);
  protected readonly paymentConfigured = signal(true);

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [templates, availability] = await Promise.all([
        firstValueFrom(this.paymentApi.listDebtTemplates()),
        firstValueFrom(this.paymentApi.getPaymentAvailability()),
      ]);
      this.templates.set(templates ?? []);
      this.paymentConfigured.set(availability.hasPaymentCredentials);
      if (!availability.hasPaymentCredentials) {
        this.matDialog.open(NoPaymentCredentialsDialog, { width: '420px' });
      }
    } catch {
      this.notifications.error('Error al cargar paquetes.');
    } finally {
      this.loading.set(false);
    }
  }

  async onPay(template: DebtTemplate): Promise<void> {
    if (!this.paymentConfigured()) {
      return;
    }

    const result = await this.dialogs.openForm<PayDialog, PayDialogData, PayDialogResult>(
      PayDialog,
      { template },
      { width: '460px' },
    );
    if (!result) {
      return;
    }

    this.paying.set(template.id);
    try {
      const queued = await firstValueFrom(this.paymentApi.createQrDebt(template.id, result.email));
      const finalStatus = await this.pollUntilSettled(queued.identificadorDeuda);

      if (finalStatus.status === 'Ready' && finalStatus.qrSimpleUrl) {
        this.openQrDialog(finalStatus.identificadorDeuda, finalStatus.qrSimpleUrl);
      } else if (finalStatus.status === 'Failed') {
        this.notifications.error(
          `Error al generar QR: ${finalStatus.error ?? 'reintente más tarde.'}`,
        );
      } else {
        this.notifications.info('Generación en curso. Revise sus pendientes en unos segundos.');
      }
    } catch {
      this.notifications.error('Error al generar QR. Intente de nuevo.');
    } finally {
      this.paying.set(null);
    }
  }

  private pollUntilSettled(debtId: string): Promise<QrDebtStatus> {
    return pollQrDebtUntilSettled(debtId, (id) => this.paymentApi.getQrDebtStatus(id));
  }

  private openQrDialog(debtId: string, qrUrl: string): void {
    this.matDialog.open<QrImageDialog, QrImageDialogData>(QrImageDialog, {
      data: { debtId, qrUrl },
      width: '400px',
    });
  }
}
