import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'money', pure: true })
export class MoneyPipe implements PipeTransform {
  private static readonly formatters = new Map<string, Intl.NumberFormat>();

  transform(amount: number | null | undefined, currency = 'BOB'): string {
    if (amount === null || amount === undefined) {
      return '—';
    }
    return MoneyPipe.formatterFor(currency).format(amount);
  }

  private static formatterFor(currency: string): Intl.NumberFormat {
    let formatter = MoneyPipe.formatters.get(currency);
    if (!formatter) {
      formatter = new Intl.NumberFormat('es-BO', {
        style: 'currency',
        currency,
        maximumFractionDigits: 2,
      });
      MoneyPipe.formatters.set(currency, formatter);
    }
    return formatter;
  }
}
