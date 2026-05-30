import { Pipe, PipeTransform, inject } from '@angular/core';

import { AuthService } from '@core/auth';

export type TenantDatePrecision = 'date' | 'datetime';

@Pipe({ name: 'tenantDate', pure: false })
export class TenantDatePipe implements PipeTransform {
  private readonly authService = inject(AuthService);
  private readonly formatterCache = new Map<string, Intl.DateTimeFormat>();

  transform(isoDate: string | null | undefined, precision: TenantDatePrecision = 'date'): string {
    if (!isoDate) {
      return '—';
    }
    const tenantTimezone = this.authService.tenantTimezone();
    return this.formatterFor(tenantTimezone, precision).format(new Date(isoDate));
  }

  private formatterFor(
    tenantTimezone: string,
    precision: TenantDatePrecision,
  ): Intl.DateTimeFormat {
    const cacheKey = `${tenantTimezone}|${precision}`;
    const cached = this.formatterCache.get(cacheKey);
    if (cached) {
      return cached;
    }
    const options: Intl.DateTimeFormatOptions =
      precision === 'datetime'
        ? {
            year: 'numeric',
            month: 'short',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            timeZone: tenantTimezone,
          }
        : {
            year: 'numeric',
            month: 'short',
            day: '2-digit',
            timeZone: tenantTimezone,
          };
    const formatter = new Intl.DateTimeFormat('es-BO', options);
    this.formatterCache.set(cacheKey, formatter);
    return formatter;
  }
}
