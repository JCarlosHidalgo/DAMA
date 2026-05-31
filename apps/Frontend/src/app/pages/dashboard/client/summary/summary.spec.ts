import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { ClientSummary } from './summary';
import { AuthApi, PaymentApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationService } from '@core/services';
import { PaymentSummary } from '@core/models';
import { buildJwtClaims } from '@testing';

const samplePayment: PaymentSummary = {
  totalEarnings: 1000,
  monthEarnings: 200,
  firstPaymentDate: '2026-01-01T00:00:00Z',
  from: '2026-04-01',
  to: '2026-04-30',
};

describe('ClientSummary', () => {
  const authApi = { updateTenantTimezone: vi.fn(() => of(undefined)) };
  const notifications = { success: vi.fn(), error: vi.fn() };

  async function instantiate(api: { getSummary: () => unknown }) {
    authApi.updateTenantTimezone.mockClear();
    notifications.success.mockClear();
    notifications.error.mockClear();
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ClientSummary],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PaymentApi, useValue: api },
        { provide: AuthApi, useValue: authApi },
        { provide: NotificationService, useValue: notifications },
        {
          provide: AuthService,
          useValue: {
            tenantTimezone: signal('America/La_Paz'),
            claims: signal(buildJwtClaims()),
          },
        },
      ],
    }).compileComponents();
    return TestBed.createComponent(ClientSummary);
  }

  it('starts in loading state', async () => {
    const fixture = await instantiate({ getSummary: () => new Subject() });
    fixture.detectChanges();
    expect(fixture.componentInstance.state()).toEqual({ kind: 'loading' });
    expect(fixture.componentInstance.ready()).toBeNull();
  });

  it('transitions to ready and exposes the summary data', async () => {
    const fixture = await instantiate({ getSummary: () => of(samplePayment) });
    fixture.detectChanges();
    expect(fixture.componentInstance.state().kind).toBe('ready');
    expect(fixture.componentInstance.ready()).toEqual(samplePayment);
  });

  it('transitions to error when the request fails', async () => {
    const fixture = await instantiate({
      getSummary: () => throwError(() => new Error('boom')),
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.state().kind).toBe('error');
    expect(fixture.componentInstance.ready()).toBeNull();
  });

  it('updates the tenant timezone when a new zone is selected', async () => {
    const fixture = await instantiate({ getSummary: () => of(samplePayment) });
    fixture.detectChanges();
    fixture.componentInstance.onTimezoneChange('America/Lima');
    expect(authApi.updateTenantTimezone).toHaveBeenCalledWith(expect.any(String), {
      timezone: 'America/Lima',
    });
  });

  it('ignores a selection equal to the current timezone', async () => {
    const fixture = await instantiate({ getSummary: () => of(samplePayment) });
    fixture.detectChanges();
    fixture.componentInstance.onTimezoneChange('America/La_Paz');
    expect(authApi.updateTenantTimezone).not.toHaveBeenCalled();
  });
});
