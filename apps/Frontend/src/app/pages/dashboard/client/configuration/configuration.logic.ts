import { TodotixAppKeyStatus } from '@core/models';

export type AppKeyState =
  | { kind: 'loading' }
  | { kind: 'ready'; status: TodotixAppKeyStatus }
  | { kind: 'error' };

export const TIMEZONE_OPTIONS = [
  'America/La_Paz',
  'America/Lima',
  'America/Bogota',
  'America/Mexico_City',
  'America/Argentina/Buenos_Aires',
  'America/Sao_Paulo',
  'America/Santiago',
  'America/New_York',
  'America/Los_Angeles',
  'Europe/Madrid',
  'Europe/London',
  'Europe/Paris',
  'Asia/Tokyo',
] as const;

export function subscriptionAllowsTodotix(subscriptionIndex: number): boolean {
  return subscriptionIndex >= 3;
}

export function asReadyAppKey(state: AppKeyState): { status: TodotixAppKeyStatus } | null {
  return state.kind === 'ready' ? state : null;
}

export function shouldUpdateTimezone(next: string, current: string): boolean {
  return next !== current;
}
