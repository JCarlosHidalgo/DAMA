import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth-service';
import { UserRole } from './jwt.model';

type MinimumSubscriptionIndex = number | Partial<Record<UserRole, number>>;

export const subscriptionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requirement = route.data?.['minSubscriptionIndex'] as MinimumSubscriptionIndex | undefined;
  const minimumIndex = resolveMinimumIndex(requirement, authService.currentRole());

  if (authService.effectiveSubscriptionIndex() >= minimumIndex) {
    return true;
  }
  return router.createUrlTree(['/yo']);
};

function resolveMinimumIndex(
  requirement: MinimumSubscriptionIndex | undefined,
  role: UserRole | null,
): number {
  if (typeof requirement === 'number') {
    return requirement;
  }
  if (requirement && role) {
    return requirement[role] ?? 0;
  }
  return 0;
}
