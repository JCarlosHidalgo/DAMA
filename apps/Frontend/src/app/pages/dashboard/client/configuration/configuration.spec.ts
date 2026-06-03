import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi, beforeEach } from 'vitest';

import { ClientConfiguration } from './configuration';
import { AuthApi, PaymentApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationService } from '@core/services';
import { TodotixAppKeyStatus } from '@core/models';
import { buildJwtClaims } from '@testing';

const sampleStatus: TodotixAppKeyStatus = {
  hasCustomKey: true,
  maskedAppKey: '••••••••2724',
};

describe('ClientConfiguration', () => {
  const paymentApi = {
    getTodotixAppKeyStatus: vi.fn(() => of(sampleStatus)),
    revealTodotixAppKey: vi.fn(() => of({ appKey: '51599bd3-eed3-2826-45a4-a16c2fcc2724' })),
    updateTodotixAppKey: vi.fn(() => of(undefined)),
    testTodotixCredential: vi.fn(() => of(undefined)),
  };
  const authApi = { updateTenantTimezone: vi.fn(() => of(undefined)) };
  const notifications = { success: vi.fn(), error: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    paymentApi.getTodotixAppKeyStatus.mockReturnValue(of(sampleStatus));
    paymentApi.revealTodotixAppKey.mockReturnValue(
      of({ appKey: '51599bd3-eed3-2826-45a4-a16c2fcc2724' }),
    );
    paymentApi.updateTodotixAppKey.mockReturnValue(of(undefined));
    paymentApi.testTodotixCredential.mockReturnValue(of(undefined));
    authApi.updateTenantTimezone.mockReturnValue(of(undefined));
  });

  async function instantiate() {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ClientConfiguration],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PaymentApi, useValue: paymentApi },
        { provide: AuthApi, useValue: authApi },
        { provide: NotificationService, useValue: notifications },
        {
          provide: AuthService,
          useValue: {
            tenantTimezone: signal('America/La_Paz'),
            claims: signal(buildJwtClaims()),
            effectiveSubscriptionIndex: signal(3),
          },
        },
      ],
    }).compileComponents();
    return TestBed.createComponent(ClientConfiguration);
  }

  it('loads the app-key status on init', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    expect(paymentApi.getTodotixAppKeyStatus).toHaveBeenCalled();
    expect(fixture.componentInstance.appKeyState()).toEqual({
      kind: 'ready',
      status: sampleStatus,
    });
  });

  it('updates the tenant timezone when a new zone is selected', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    fixture.componentInstance.onTimezoneChange('America/Lima');
    expect(authApi.updateTenantTimezone).toHaveBeenCalledWith(expect.any(String), {
      timezone: 'America/Lima',
    });
  });

  it('ignores a timezone selection equal to the current one', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    fixture.componentInstance.onTimezoneChange('America/La_Paz');
    expect(authApi.updateTenantTimezone).not.toHaveBeenCalled();
  });

  it('reveals and hides the full app-key', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    fixture.componentInstance.toggleReveal();
    expect(paymentApi.revealTodotixAppKey).toHaveBeenCalled();
    expect(fixture.componentInstance.revealedKey()).toBe('51599bd3-eed3-2826-45a4-a16c2fcc2724');
    fixture.componentInstance.toggleReveal();
    expect(fixture.componentInstance.revealedKey()).toBeNull();
  });

  it('rejects an app-key that is not a lowercase GUID', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    fixture.componentInstance.form.controls.appKey.setValue('NOT-A-GUID');
    expect(fixture.componentInstance.form.invalid).toBe(true);
    fixture.componentInstance.saveAppKey();
    expect(paymentApi.updateTodotixAppKey).not.toHaveBeenCalled();
  });

  it('saves a valid app-key and reloads the status', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    paymentApi.getTodotixAppKeyStatus.mockClear();
    fixture.componentInstance.form.controls.appKey.setValue('51599bd3-eed3-2826-45a4-a16c2fcc2724');
    fixture.componentInstance.saveAppKey();
    expect(paymentApi.updateTodotixAppKey).toHaveBeenCalledWith({
      appKey: '51599bd3-eed3-2826-45a4-a16c2fcc2724',
    });
    expect(notifications.success).toHaveBeenCalled();
    expect(paymentApi.getTodotixAppKeyStatus).toHaveBeenCalled();
  });

  it('tests the saved credential and shows the success toast', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    fixture.componentInstance.testCredential();
    expect(paymentApi.testTodotixCredential).toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalledWith('La credencial funciona');
    expect(fixture.componentInstance.testing()).toBe(false);
  });

  it('shows an error toast when the credential test fails', async () => {
    const fixture = await instantiate();
    fixture.detectChanges();
    paymentApi.testTodotixCredential.mockReturnValueOnce(throwError(() => new Error('fail')));
    fixture.componentInstance.testCredential();
    expect(notifications.error).toHaveBeenCalled();
    expect(notifications.success).not.toHaveBeenCalled();
    expect(fixture.componentInstance.testing()).toBe(false);
  });
});
