export function normalizeOptionalEmail(email: string): string | null {
  return email.trim() === '' ? null : email.trim();
}
