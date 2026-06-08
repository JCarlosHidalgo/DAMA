import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { AuthService, roleGuard, subscriptionGuard, UserRole } from '@core/auth';
import { defaultRouteForRole } from '@core/router';

const dashboardChildRoutes: Routes = [
  {
    path: 'resumen',
    canActivate: [roleGuard, subscriptionGuard],
    data: {
      roles: ['Client', 'Student'] as UserRole[],
      minSubscriptionIndex: { Client: 0, Student: 2 },
    },
    loadComponent: () => import('./shared/summary-router').then((m) => m.SummaryRouter),
  },
  {
    path: 'recarga',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Client'] as UserRole[], minSubscriptionIndex: 3 },
    loadComponent: () => import('./client/recharge/recharge').then((m) => m.Recharge),
  },
  {
    path: 'clases',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Client'] as UserRole[], minSubscriptionIndex: 1 },
    loadComponent: () => import('./client/courses/courses').then((m) => m.Courses),
  },
  {
    path: 'horario',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Client', 'Teacher', 'Student'] as UserRole[], minSubscriptionIndex: 1 },
    loadComponent: () => import('./shared/schedule-router').then((m) => m.ScheduleRouter),
  },
  {
    path: 'plantillas-cobro',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Client'] as UserRole[], minSubscriptionIndex: 3 },
    loadComponent: () =>
      import('./client/debt-templates/debt-templates').then((m) => m.DebtTemplates),
  },
  {
    path: 'estudiantes',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Client'] as UserRole[], minSubscriptionIndex: 2 },
    loadComponent: () => import('./client/students/students').then((m) => m.Students),
  },
  {
    path: 'profesores',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Client'] as UserRole[], minSubscriptionIndex: 2 },
    loadComponent: () => import('./client/teachers/teachers').then((m) => m.Teachers),
  },
  {
    path: 'configuracion',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () =>
      import('./client/configuration/configuration').then((m) => m.ClientConfiguration),
  },
  {
    path: 'pagar-clases',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Student'] as UserRole[], minSubscriptionIndex: 3 },
    loadComponent: () => import('./student/pay-classes/pay-classes').then((m) => m.PayClasses),
  },
  {
    path: 'estado-deudas',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Student'] as UserRole[], minSubscriptionIndex: 3 },
    loadComponent: () => import('./student/debt-status/debt-status').then((m) => m.DebtStatus),
  },
  {
    path: 'marcar-asistencia',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Student'] as UserRole[], minSubscriptionIndex: 2 },
    loadComponent: () =>
      import('./student/mark-attendance/mark-attendance').then((m) => m.MarkAttendance),
  },
  {
    path: 'mis-asistencias',
    canActivate: [roleGuard, subscriptionGuard],
    data: { roles: ['Student'] as UserRole[], minSubscriptionIndex: 2 },
    loadComponent: () =>
      import('./student/attendance-history/attendance-history').then((m) => m.AttendanceHistory),
  },
  {
    path: 'suscripcion',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () =>
      import('./client/subscription/subscription').then((m) => m.ClientSubscription),
  },
  {
    path: 'tenants',
    canActivate: [roleGuard],
    data: { roles: ['Admin'] as UserRole[] },
    loadComponent: () => import('./admin/tenants/tenants').then((m) => m.Tenants),
  },
  {
    path: 'planes-suscripcion',
    canActivate: [roleGuard],
    data: { roles: ['Admin'] as UserRole[] },
    loadComponent: () =>
      import('./admin/subscription-plans/subscription-plans').then((m) => m.AdminSubscriptionPlans),
  },
  {
    path: 'analisis',
    canActivate: [roleGuard],
    data: { roles: ['Admin'] as UserRole[] },
    loadComponent: () => import('./admin/analytics/analytics').then((m) => m.AdminAnalytics),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: () => {
      const role = inject(AuthService).currentRole();
      return role ? defaultRouteForRole(role) : '/';
    },
  },
];

export const dashboardRoutes: Routes = [
  {
    path: '',
    providers: [
      provideTanStackQuery(
        new QueryClient({
          defaultOptions: {
            queries: {
              staleTime: 60_000,
              gcTime: 5 * 60_000,
              refetchOnWindowFocus: false,
              retry: 1,
            },
          },
        }),
      ),
      provideCharts(withDefaultRegisterables()),
    ],
    children: dashboardChildRoutes,
  },
];
