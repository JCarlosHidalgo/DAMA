import { Observable, of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { pollQrDebtUntilSettled } from './qr-debt-polling';
import { QrDebtStatus } from '@core/models';

function status(value: QrDebtStatus['status'], extra: Partial<QrDebtStatus> = {}): QrDebtStatus {
  return { identificadorDeuda: 'debt-1', status: value, ...extra };
}

describe('pollQrDebtUntilSettled', () => {
  it('returns on the first attempt when the status is already settled (default options)', async () => {
    const fetchStatus = vi.fn<(debtId: string) => Observable<QrDebtStatus>>(() =>
      of(status('Ready', { qrSimpleUrl: 'http://qr' })),
    );

    const result = await pollQrDebtUntilSettled('debt-1', fetchStatus);

    expect(result.status).toBe('Ready');
    expect(fetchStatus).toHaveBeenCalledTimes(1);
    expect(fetchStatus).toHaveBeenCalledWith('debt-1');
  });

  it('treats a fetch error as transient and keeps polling until it settles', async () => {
    const fetchStatus = vi
      .fn<(debtId: string) => Observable<QrDebtStatus>>()
      .mockReturnValueOnce(throwError(() => new Error('network')))
      .mockReturnValueOnce(of(status('Failed', { error: 'x' })));

    const result = await pollQrDebtUntilSettled('debt-1', fetchStatus, {
      intervalMilliseconds: 1,
      maxAttempts: 5,
    });

    expect(result.status).toBe('Failed');
    expect(fetchStatus).toHaveBeenCalledTimes(2);
  });

  it('returns the last Pending status after exhausting all attempts', async () => {
    const fetchStatus = vi.fn<(debtId: string) => Observable<QrDebtStatus>>(() =>
      of(status('Pending')),
    );

    const result = await pollQrDebtUntilSettled('debt-1', fetchStatus, {
      intervalMilliseconds: 1,
      maxAttempts: 3,
    });

    expect(result.status).toBe('Pending');
    expect(fetchStatus).toHaveBeenCalledTimes(3);
  });
});
