import { SubscriptionPlan } from '@core/models';

export function sortPlansByLevel(plans: SubscriptionPlan[] | null | undefined): SubscriptionPlan[] {
  return [...(plans ?? [])].sort((first, second) => first.level - second.level);
}
