import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth-service';

export const subscriptionAccessGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const role = authService.currentRole();
  if (
    (role === 'Teacher' || role === 'Student') &&
    authService.effectiveSubscriptionIndex() === 0
  ) {
    authService.clearSession();
    return router.createUrlTree(['']);
  }
  return true;
};
