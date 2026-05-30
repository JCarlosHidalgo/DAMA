export function nowInTenant(tenantTimezone: string): Date {
  const nowUtc = new Date();
  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: tenantTimezone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });
  const dateParts = formatter.formatToParts(nowUtc);
  const partValue = (type: string) => dateParts.find((part) => part.type === type)?.value ?? '00';
  return new Date(
    `${partValue('year')}-${partValue('month')}-${partValue('day')}T${partValue('hour')}:${partValue('minute')}:${partValue('second')}`,
  );
}

export function todayDateOnlyInTenant(tenantTimezone: string): string {
  const localDate = nowInTenant(tenantTimezone);
  const year = localDate.getFullYear();
  const month = String(localDate.getMonth() + 1).padStart(2, '0');
  const day = String(localDate.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
