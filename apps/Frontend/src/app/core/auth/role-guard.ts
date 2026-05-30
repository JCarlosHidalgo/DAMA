import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth-service';
import { UserRole } from './jwt.model';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const currentRole = authService.currentRole();

  if (!currentRole) {
    return router.createUrlTree(['']);
  }

  const allowedRoles = (route.data?.['roles'] ?? []) as UserRole[];
  if (allowedRoles.length === 0 || allowedRoles.includes(currentRole)) {
    return true;
  }
  return router.createUrlTree(['/yo']);
};
