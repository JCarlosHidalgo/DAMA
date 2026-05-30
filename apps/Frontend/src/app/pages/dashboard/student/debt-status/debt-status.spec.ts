import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { DebtStatus } from './debt-status';
import { PaymentApi } from '../../../../core/api/payment.api';
import { AuthService } from '../../../../core/auth/auth-service';
import { NotificationService } from '../../../../core/services/notification-service';
import { Page } from '../../../../core/models/page.model';

function page<T>(items: T[]): Page<T> {
  return { currentIndex: 0, maxIndex: 0, items };
}

describe('DebtStatus', () => {
  let paymentApi: {
    listPendingQr: ReturnType<typeof vi.fn>;
    listSuccessQr: ReturnType<typeof vi.fn>;
    listFailedQr: ReturnType<typeof vi.fn>;
  };
  let notifications: { error: ReturnType<typeof vi.fn> };

  async function setUp() {
    TestBed.resetTestingModule();
    paymentApi = {
      listPendingQr: vi.fn(() => of(page([{ id: 'p1' }]))),
      listSuccessQr: vi.fn(() => of(page([{ id: 's1' }]))),
      listFailedQr: vi.fn(() => of(page([{ id: 'f1' }]))),
    };
    notifications = { error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [DebtStatus],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PaymentApi, useValue: paymentApi },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: { tenantTimezone: signal('America/La_Paz') } },
      ],
    }).compileComponents();

    return TestBed.createComponent(DebtStatus);
  }

  async function flush(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
  }

  it('loads the pending tab on construction', async () => {
    const fixture = await setUp();
    await flush();

    expect(paymentApi.listPendingQr).toHaveBeenCalledWith(0);
    expect(fixture.componentInstance['pending'].state().page?.items).toEqual([{ id: 'p1' }]);
  });

  it('lazily loads a tab the first time it is selected, then caches it', async () => {
    const fixture = await setUp();
    await flush();

    fixture.componentInstance.onTabChange(1);
    await flush();
    expect(paymentApi.listSuccessQr).toHaveBeenCalledTimes(1);

    fixture.componentInstance.onTabChange(1);
    await flush();
    expect(paymentApi.listSuccessQr).toHaveBeenCalledTimes(1);
  });

  it('routes changePage to the matching endpoint and page index', async () => {
    const fixture = await setUp();
    await flush();

    await fixture.componentInstance.changePage('failed', 2);

    expect(paymentApi.listFailedQr).toHaveBeenCalledWith(2);
  });

  it('notifies when a tab fails to load', async () => {
    const fixture = await setUp();
    paymentApi.listSuccessQr.mockReturnValue(throwError(() => new Error('down')));
    await flush();

    await fixture.componentInstance.changePage('success', 0);

    expect(notifications.error).toHaveBeenCalledWith('Error al cargar lista.');
  });
});
