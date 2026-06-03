import {
  SubscriptionDurationUnit,
  SubscriptionPlan,
  UpdateSubscriptionPlanPayload,
} from '@core/models';

export const DURATION_UNITS: SubscriptionDurationUnit[] = ['Day', 'Week', 'Month'];

export function subscriptionUnitLabel(unit: SubscriptionDurationUnit): string {
  return { Day: 'Días', Week: 'Semanas', Month: 'Meses' }[unit];
}

export function sortPlansByLevel(plans: SubscriptionPlan[] | null | undefined): SubscriptionPlan[] {
  return [...(plans ?? [])].sort((first, second) => first.level - second.level);
}

export function subscriptionPlanUpdatePayload(value: {
  price: number;
  durationAmount: number;
  durationUnit: string;
}): UpdateSubscriptionPlanPayload {
  return {
    price: Number(value.price),
    durationAmount: Number(value.durationAmount),
    durationUnit: value.durationUnit as SubscriptionDurationUnit,
  };
}

export function planUpdatedMessage(level: number): string {
  return `Plan nivel ${level} actualizado.`;
}
