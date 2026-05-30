import { Routes } from '@angular/router';

import { roleGuard, UserRole } from '@core/auth';

export const dashboardRoutes: Routes = [
  {
    path: 'resumen',
    canActivate: [roleGuard],
    data: { roles: ['Client', 'Student'] as UserRole[] },
    loadComponent: () => import('./shared/summary-router').then((m) => m.SummaryRouter),
  },
  {
    path: 'recarga',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () => import('./client/recharge/recharge').then((m) => m.Recharge),
  },
  {
    path: 'clases',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () => import('./client/courses/courses').then((m) => m.Courses),
  },
  {
    path: 'horario',
    canActivate: [roleGuard],
    data: { roles: ['Client', 'Teacher'] as UserRole[] },
    loadComponent: () => import('./shared/schedule-router').then((m) => m.ScheduleRouter),
  },
  {
    path: 'plantillas-cobro',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () =>
      import('./client/debt-templates/debt-templates').then((m) => m.DebtTemplates),
  },
  {
    path: 'estudiantes',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () => import('./client/students/students').then((m) => m.Students),
  },
  {
    path: 'profesores',
    canActivate: [roleGuard],
    data: { roles: ['Client'] as UserRole[] },
    loadComponent: () => import('./client/teachers/teachers').then((m) => m.Teachers),
  },
  {
    path: 'pagar-clases',
    canActivate: [roleGuard],
    data: { roles: ['Student'] as UserRole[] },
    loadComponent: () => import('./student/pay-classes/pay-classes').then((m) => m.PayClasses),
  },
  {
    path: 'estado-deudas',
    canActivate: [roleGuard],
    data: { roles: ['Student'] as UserRole[] },
    loadComponent: () => import('./student/debt-status/debt-status').then((m) => m.DebtStatus),
  },
  {
    path: 'marcar-asistencia',
    canActivate: [roleGuard],
    data: { roles: ['Student'] as UserRole[] },
    loadComponent: () =>
      import('./student/mark-attendance/mark-attendance').then((m) => m.MarkAttendance),
  },
  {
    path: 'mis-asistencias',
    canActivate: [roleGuard],
    data: { roles: ['Student'] as UserRole[] },
    loadComponent: () =>
      import('./student/attendance-history/attendance-history').then((m) => m.AttendanceHistory),
  },
  { path: '', pathMatch: 'full', redirectTo: 'resumen' },
];

export function defaultRouteForRole(role: UserRole): string {
  switch (role) {
    case 'Client':
      return '/yo/resumen';
    case 'Teacher':
      return '/yo/horario';
    case 'Student':
      return '/yo/resumen';
    case 'Admin':
      return '/';
  }
}
