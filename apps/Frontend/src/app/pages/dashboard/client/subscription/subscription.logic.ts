import { SubscriptionPlan } from '@core/models';

export const LEVEL_LABELS: Record<number, string> = {
  1: 'Base — cursos y clases',
  2: 'Intermedio — + estudiantes, profesores y asistencia',
  3: 'Completo — + gestión de pagos',
};

export const DURATION_UNIT_LABELS: Record<string, string> = {
  Day: 'día(s)',
  Week: 'semana(s)',
  Month: 'mes(es)',
};

export function describePlanDuration(plan: SubscriptionPlan): string {
  return `${plan.durationAmount} ${DURATION_UNIT_LABELS[plan.durationUnit] ?? plan.durationUnit}`;
}

export function subscriptionLevelLabel(level: number): string {
  return LEVEL_LABELS[level] ?? `Nivel ${level}`;
}

export function subscriptionExpiresLabel(expiresAtEpochSeconds: number): string {
  if (expiresAtEpochSeconds <= 0) {
    return '—';
  }
  return new Date(expiresAtEpochSeconds * 1000).toLocaleDateString('es', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

export function subscriptionPayConfirmMessage(level: number): string {
  return `¿Registrar la deuda para el nivel ${level}?`;
}
