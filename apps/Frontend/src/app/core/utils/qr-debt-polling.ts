import { Observable, catchError, firstValueFrom, of, timer } from 'rxjs';

import { QrDebtStatus } from '../models/payment.model';

export interface QrDebtPollingOptions {
  intervalMilliseconds: number;
  maxAttempts: number;
}

export const DEFAULT_QR_DEBT_POLLING: QrDebtPollingOptions = {
  intervalMilliseconds: 750,
  maxAttempts: 40,
};

export async function pollQrDebtUntilSettled(
  debtId: string,
  fetchStatus: (debtId: string) => Observable<QrDebtStatus>,
  options: QrDebtPollingOptions = DEFAULT_QR_DEBT_POLLING,
): Promise<QrDebtStatus> {
  let latestStatus: QrDebtStatus = { identificadorDeuda: debtId, status: 'Pending' };
  for (let attemptIndex = 0; attemptIndex < options.maxAttempts; attemptIndex++) {
    latestStatus = await firstValueFrom(
      fetchStatus(debtId).pipe(catchError(() => of(latestStatus))),
    );
    if (latestStatus.status !== 'Pending') {
      return latestStatus;
    }
    await firstValueFrom(timer(options.intervalMilliseconds));
  }
  return latestStatus;
}
