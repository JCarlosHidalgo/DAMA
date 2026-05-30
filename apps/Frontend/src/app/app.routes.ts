import { Routes } from '@angular/router';
import { authGuard } from '@core/auth';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'yo',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
    loadChildren: () => import('./pages/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
  },
  { path: '**', redirectTo: '' },
];
