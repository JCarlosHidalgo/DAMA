import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'money', pure: true })
export class MoneyPipe implements PipeTransform {
  private static readonly formatter = new Intl.NumberFormat('es-BO', {
    style: 'currency',
    currency: 'BOB',
    maximumFractionDigits: 2,
  });

  transform(amount: number | null | undefined): string {
    if (amount === null || amount === undefined) {
      return '—';
    }
    return MoneyPipe.formatter.format(amount);
  }
}
