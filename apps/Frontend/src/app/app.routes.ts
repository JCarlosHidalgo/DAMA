import { Routes } from '@angular/router';
import { authGuard, subscriptionAccessGuard } from '@core/auth';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'yo',
    canActivate: [authGuard, subscriptionAccessGuard],
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
    loadChildren: () => import('./pages/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
  },
  { path: '**', redirectTo: '' },
];
