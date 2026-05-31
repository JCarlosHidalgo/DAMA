import { Injectable, inject } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { EMPTY, Observable } from 'rxjs';

import { AuthService, UserRole } from '@core/auth';

interface IdleWindow {
  requestIdleCallback?: (callback: () => void, options?: { timeout: number }) => number;
  cancelIdleCallback?: (handle: number) => void;
}

@Injectable({ providedIn: 'root' })
export class RoleAwarePreloadStrategy implements PreloadingStrategy {
  private readonly authService = inject(AuthService);

  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    const currentRole = this.authService.currentRole();
    if (!currentRole) {
      return EMPTY;
    }

    const requiredRoles = (route.data?.['roles'] ?? []) as UserRole[];
    if (requiredRoles.length > 0 && !requiredRoles.includes(currentRole)) {
      return EMPTY;
    }

    return new Observable((subscriber) => {
      const idleWindow = globalThis as unknown as IdleWindow;
      let idleHandle: number | null = null;
      let timeoutHandle: ReturnType<typeof setTimeout> | null = null;

      const startLoad = () => {
        idleHandle = null;
        timeoutHandle = null;
        load().subscribe({
          next: (value) => subscriber.next(value),
          error: (error) => subscriber.error(error),
          complete: () => subscriber.complete(),
        });
      };

      if (idleWindow.requestIdleCallback) {
        idleHandle = idleWindow.requestIdleCallback(startLoad, { timeout: 5_000 });
      } else {
        timeoutHandle = setTimeout(startLoad, 1_000);
      }

      return () => {
        if (idleHandle !== null && idleWindow.cancelIdleCallback) {
          idleWindow.cancelIdleCallback(idleHandle);
        }
        if (timeoutHandle !== null) {
          clearTimeout(timeoutHandle);
        }
      };
    });
  }
}
