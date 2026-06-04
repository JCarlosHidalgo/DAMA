import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { of } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { DebtTemplates, DebtTemplateDialog } from './debt-templates';
import { PaymentApi } from '@core/api';
import { DialogService, NotificationService } from '@core/services';
import { DebtTemplate } from '@core/models';

const TEMPLATE: DebtTemplate = {
  id: 'template-1',
  description: 'Pack 10',
  classQuantity: 10,
  cost: 250,
  currency: 'BOB',
  tenantId: 'tenant-1',
};

async function flushAsync(): Promise<void> {
  await new Promise<void>((resolve) => setTimeout(resolve, 0));
}

describe('DebtTemplateDialog', () => {
  async function createDialog(data: { mode: 'create' | 'edit'; initial?: Partial<DebtTemplate> }) {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [DebtTemplateDialog],
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();
    return TestBed.createComponent(DebtTemplateDialog);
  }

  it('starts invalid in create mode because the description is required', async () => {
    const fixture = await createDialog({ mode: 'create' });
    expect(fixture.componentInstance['form'].invalid).toBe(true);
  });

  it('becomes valid once a description and positive numbers are set', async () => {
    const fixture = await createDialog({ mode: 'create' });
    fixture.componentInstance['form'].setValue({
      description: 'Pack 5',
      classQuantity: 5,
      cost: 100,
    });
    expect(fixture.componentInstance['form'].valid).toBe(true);
  });

  it('rejects a cost below 1', async () => {
    const fixture = await createDialog({ mode: 'create' });
    const form = fixture.componentInstance['form'];
    form.setValue({ description: 'x', classQuantity: 1, cost: 0 });
    expect(form.controls.cost.hasError('min')).toBe(true);
  });

  it('rejects a class quantity below 1', async () => {
    const fixture = await createDialog({ mode: 'create' });
    const form = fixture.componentInstance['form'];
    form.setValue({ description: 'x', classQuantity: 0, cost: 1 });
    expect(form.controls.classQuantity.hasError('min')).toBe(true);
  });

  it('rejects a description longer than 256 characters', async () => {
    const fixture = await createDialog({ mode: 'create' });
    const form = fixture.componentInstance['form'];
    form.setValue({ description: 'a'.repeat(257), classQuantity: 1, cost: 1 });
    expect(form.controls.description.hasError('maxlength')).toBe(true);
  });

  it('prefills the form from the initial values in edit mode', async () => {
    const fixture = await createDialog({
      mode: 'edit',
      initial: { description: 'Pack 10', classQuantity: 10, cost: 250 },
    });
    expect(fixture.componentInstance['form'].getRawValue()).toEqual({
      description: 'Pack 10',
      classQuantity: 10,
      cost: 250,
    });
  });
});

describe('DebtTemplates', () => {
  let paymentApi: {
    listDebtTemplates: ReturnType<typeof vi.fn>;
    createDebtTemplate: ReturnType<typeof vi.fn>;
    updateDebtTemplate: ReturnType<typeof vi.fn>;
    deleteDebtTemplate: ReturnType<typeof vi.fn>;
  };
  let dialogs: { openForm: ReturnType<typeof vi.fn>; confirm: ReturnType<typeof vi.fn> };
  let notifications: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  async function setUp() {
    TestBed.resetTestingModule();
    paymentApi = {
      listDebtTemplates: vi.fn(() => of([TEMPLATE])),
      createDebtTemplate: vi.fn(() => of(TEMPLATE)),
      updateDebtTemplate: vi.fn(() => of(undefined)),
      deleteDebtTemplate: vi.fn(() => of(undefined)),
    };
    dialogs = { openForm: vi.fn(), confirm: vi.fn() };
    notifications = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [DebtTemplates],
      providers: [
        provideZonelessChangeDetection(),
        provideTanStackQuery(new QueryClient()),
        { provide: PaymentApi, useValue: paymentApi },
        { provide: DialogService, useValue: dialogs },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    return TestBed.createComponent(DebtTemplates);
  }

  it('does not create when the dialog is cancelled', async () => {
    const fixture = await setUp();
    dialogs.openForm.mockResolvedValue(undefined);

    await fixture.componentInstance.onCreate();
    await flushAsync();

    expect(paymentApi.createDebtTemplate).not.toHaveBeenCalled();
  });

  it('creates with the dialog result payload', async () => {
    const fixture = await setUp();
    dialogs.openForm.mockResolvedValue({ description: 'Pack 5', classQuantity: 5, cost: 100 });

    await fixture.componentInstance.onCreate();
    await flushAsync();

    expect(paymentApi.createDebtTemplate).toHaveBeenCalledWith({
      description: 'Pack 5',
      classQuantity: 5,
      cost: 100,
    });
  });

  it('does not update when the edit dialog is cancelled', async () => {
    const fixture = await setUp();
    dialogs.openForm.mockResolvedValue(undefined);

    await fixture.componentInstance.onEdit(TEMPLATE);
    await flushAsync();

    expect(paymentApi.updateDebtTemplate).not.toHaveBeenCalled();
  });

  it('does not delete a billing template unless the user confirms', async () => {
    const fixture = await setUp();
    dialogs.confirm.mockResolvedValue(false);

    await fixture.componentInstance.onDelete(TEMPLATE);
    await flushAsync();

    expect(paymentApi.deleteDebtTemplate).not.toHaveBeenCalled();
  });

  it('deletes once the user confirms', async () => {
    const fixture = await setUp();
    dialogs.confirm.mockResolvedValue(true);

    await fixture.componentInstance.onDelete(TEMPLATE);
    await flushAsync();

    expect(paymentApi.deleteDebtTemplate).toHaveBeenCalledWith('template-1');
  });
});
