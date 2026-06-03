import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

import { ErrorMapOptions, HttpErrorMapper } from './http-error-mapper';

const DEFAULT_DURATION_MS = 4000;
const DEFAULT_ACTION_LABEL = 'OK';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly errorMapper = inject(HttpErrorMapper);

  errorFrom(error: unknown, fallbackOrOptions?: string | ErrorMapOptions): void {
    const options =
      typeof fallbackOrOptions === 'string' ? { fallback: fallbackOrOptions } : fallbackOrOptions;
    this.error(this.errorMapper.mapError(error, options));
  }

  success(message: string, options?: Partial<MatSnackBarConfig>): void {
    this.show(message, {
      duration: DEFAULT_DURATION_MS,
      panelClass: 'dama-snack-success',
      ...options,
    });
  }

  error(message: string, options?: Partial<MatSnackBarConfig>): void {
    this.show(message, {
      duration: DEFAULT_DURATION_MS,
      panelClass: 'dama-snack-error',
      ...options,
    });
  }

  info(message: string, options?: Partial<MatSnackBarConfig>): void {
    this.show(message, { duration: DEFAULT_DURATION_MS, ...options });
  }

  private show(message: string, config: MatSnackBarConfig): void {
    this.snackBar.open(message, DEFAULT_ACTION_LABEL, config);
  }
}
