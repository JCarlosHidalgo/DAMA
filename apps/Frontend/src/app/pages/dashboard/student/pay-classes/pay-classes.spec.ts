import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { PayClasses, PayDialog, QrImageDialog, NoPaymentCredentialsDialog } from './pay-classes';
import { PaymentApi } from '@core/api';
import { DialogService, NotificationService } from '@core/services';
import { DebtTemplate, QrDebtStatus } from '@core/models';

const TEMPLATE: DebtTemplate = {
  id: 'template-1',
  description: 'Pack 10',
  classQuantity: 10,
  cost: 250,
  tenantId: 'tenant-1',
};

function ready(qrUrl: string): QrDebtStatus {
  return { identificadorDeuda: 'debt-1', status: 'Ready', qrSimpleUrl: qrUrl };
}

function pending(): QrDebtStatus {
  return { identificadorDeuda: 'debt-1', status: 'Pending' };
}

describe('PayClasses', () => {
  let paymentApi: {
    listDebtTemplates: ReturnType<typeof vi.fn>;
    getPaymentAvailability: ReturnType<typeof vi.fn>;
    createQrDebt: ReturnType<typeof vi.fn>;
    getQrDebtStatus: ReturnType<typeof vi.fn>;
  };
  let dialogs: { openForm: ReturnType<typeof vi.fn> };
  let matDialog: { open: ReturnType<typeof vi.fn> };
  let notifications: {
    success: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
    info: ReturnType<typeof vi.fn>;
  };

  async function setUp() {
    TestBed.resetTestingModule();
    paymentApi = {
      listDebtTemplates: vi.fn(() => of([TEMPLATE])),
      getPaymentAvailability: vi.fn(() => of({ hasPaymentCredentials: true })),
      createQrDebt: vi.fn(() =>
        of({ identificadorDeuda: 'debt-1', status: 'Pending', alreadyGenerated: false }),
      ),
      getQrDebtStatus: vi.fn(() => of(pending())),
    };
    dialogs = { openForm: vi.fn() };
    matDialog = { open: vi.fn() };
    notifications = { success: vi.fn(), error: vi.fn(), info: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [PayClasses],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PaymentApi, useValue: paymentApi },
        { provide: DialogService, useValue: dialogs },
        { provide: MatDialog, useValue: matDialog },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    return TestBed.createComponent(PayClasses);
  }

  async function flushMicrotasks(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
  }

  describe('onPay', () => {
    it('does nothing when the pay dialog is cancelled', async () => {
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue(undefined);

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(paymentApi.createQrDebt).not.toHaveBeenCalled();
      expect(fixture.componentInstance['paying']()).toBeNull();
    });

    it('opens the QR image dialog when generation settles Ready', async () => {
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue({ method: 'qr', email: 'a@b.com' });
      paymentApi.getQrDebtStatus.mockReturnValue(of(ready('http://qr/1')));

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(paymentApi.createQrDebt).toHaveBeenCalledWith('template-1', 'a@b.com');
      expect(matDialog.open).toHaveBeenCalledWith(QrImageDialog, {
        data: { debtId: 'debt-1', qrUrl: 'http://qr/1' },
        width: '400px',
      });
      expect(fixture.componentInstance['paying']()).toBeNull();
    });

    it('shows the info toast and opens the QR when the debt was already generated', async () => {
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue({ method: 'qr', email: null });
      paymentApi.createQrDebt.mockReturnValue(
        of({ identificadorDeuda: 'debt-1', status: 'Pending', alreadyGenerated: true }),
      );
      paymentApi.getQrDebtStatus.mockReturnValue(of(ready('http://qr/existing')));

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(notifications.info).toHaveBeenCalledWith(
        expect.stringContaining('Ya tenías una deuda pendiente'),
      );
      expect(matDialog.open).toHaveBeenCalledWith(QrImageDialog, {
        data: { debtId: 'debt-1', qrUrl: 'http://qr/existing' },
        width: '400px',
      });
    });

    it('forwards a null email to createQrDebt when none is given', async () => {
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue({ method: 'qr', email: null });
      paymentApi.getQrDebtStatus.mockReturnValue(of(ready('http://qr/2')));

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(paymentApi.createQrDebt).toHaveBeenCalledWith('template-1', null);
    });

    it('shows an error notification when generation settles Failed', async () => {
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue({ method: 'qr', email: null });
      paymentApi.getQrDebtStatus.mockReturnValue(
        of({ identificadorDeuda: 'debt-1', status: 'Failed', error: 'rechazado' }),
      );

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(notifications.error).toHaveBeenCalledWith(expect.stringContaining('rechazado'));
      expect(matDialog.open).not.toHaveBeenCalled();
      expect(fixture.componentInstance['paying']()).toBeNull();
    });

    it('shows an info notification when generation is still pending after the timeout', async () => {
      vi.useFakeTimers();
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue({ method: 'qr', email: null });
      paymentApi.getQrDebtStatus.mockReturnValue(of(pending()));

      const promise = fixture.componentInstance.onPay(TEMPLATE);
      await vi.runAllTimersAsync();
      await promise;

      expect(notifications.info).toHaveBeenCalled();
      expect(matDialog.open).not.toHaveBeenCalled();
      expect(fixture.componentInstance['paying']()).toBeNull();
      vi.useRealTimers();
    });

    it('shows an error and resets the paying flag when createQrDebt throws', async () => {
      const fixture = await setUp();
      dialogs.openForm.mockResolvedValue({ method: 'qr', email: null });
      paymentApi.createQrDebt.mockReturnValue(throwError(() => new Error('boom')));

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(notifications.error).toHaveBeenCalledWith(
        expect.stringContaining('Error al generar QR'),
      );
      expect(fixture.componentInstance['paying']()).toBeNull();
    });
  });

  describe('payment availability', () => {
    it('keeps payments enabled and shows no dialog when the tenant has credentials', async () => {
      const fixture = await setUp();
      await flushMicrotasks();

      expect(fixture.componentInstance['paymentConfigured']()).toBe(true);
      expect(matDialog.open).not.toHaveBeenCalled();
    });

    it('disables payments and opens the dialog when the tenant has no credentials', async () => {
      const fixture = await setUp();
      paymentApi.getPaymentAvailability.mockReturnValue(of({ hasPaymentCredentials: false }));
      fixture.componentInstance['load']();
      await flushMicrotasks();

      expect(fixture.componentInstance['paymentConfigured']()).toBe(false);
      expect(matDialog.open).toHaveBeenCalledWith(NoPaymentCredentialsDialog, { width: '420px' });
    });

    it('does not start a payment when credentials are missing', async () => {
      const fixture = await setUp();
      paymentApi.getPaymentAvailability.mockReturnValue(of({ hasPaymentCredentials: false }));
      fixture.componentInstance['load']();
      await flushMicrotasks();

      await fixture.componentInstance.onPay(TEMPLATE);

      expect(dialogs.openForm).not.toHaveBeenCalled();
      expect(paymentApi.createQrDebt).not.toHaveBeenCalled();
    });
  });

  describe('load', () => {
    it('notifies on failure to load templates', async () => {
      TestBed.resetTestingModule();
      paymentApi = {
        listDebtTemplates: vi.fn(() => throwError(() => new Error('down'))),
        getPaymentAvailability: vi.fn(() => of({ hasPaymentCredentials: true })),
        createQrDebt: vi.fn(),
        getQrDebtStatus: vi.fn(),
      };
      dialogs = { openForm: vi.fn() };
      matDialog = { open: vi.fn() };
      notifications = { success: vi.fn(), error: vi.fn(), info: vi.fn() };

      await TestBed.configureTestingModule({
        imports: [PayClasses],
        providers: [
          provideZonelessChangeDetection(),
          { provide: PaymentApi, useValue: paymentApi },
          { provide: DialogService, useValue: dialogs },
          { provide: MatDialog, useValue: matDialog },
          { provide: NotificationService, useValue: notifications },
        ],
      }).compileComponents();

      const fixture = TestBed.createComponent(PayClasses);
      await flushMicrotasks();

      expect(notifications.error).toHaveBeenCalledWith('Error al cargar paquetes.');
      expect(fixture.componentInstance['loading']()).toBe(false);
    });
  });
});

describe('PayDialog', () => {
  let dialogRef: { close: ReturnType<typeof vi.fn> };

  async function createDialog() {
    TestBed.resetTestingModule();
    dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
      imports: [PayDialog],
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { template: TEMPLATE } },
      ],
    }).compileComponents();
    return TestBed.createComponent(PayDialog);
  }

  it('closes with a null email when the field is left empty', async () => {
    const fixture = await createDialog();

    fixture.componentInstance.submit();

    expect(dialogRef.close).toHaveBeenCalledWith({ method: 'qr', email: null });
  });

  it('trims the email before closing', async () => {
    const fixture = await createDialog();
    fixture.componentInstance['form'].controls.email.setValue('  a@b.com  ');

    fixture.componentInstance.submit();

    expect(dialogRef.close).toHaveBeenCalledWith({ method: 'qr', email: 'a@b.com' });
  });
});
