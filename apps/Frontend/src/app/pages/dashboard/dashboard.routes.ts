import { inject } from '@angular/core';
import { Routes } from '@angular/router';

import { AuthService, roleGuard, UserRole } from '@core/auth';
import { defaultRouteForRole } from '@core/router';

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
    data: { roles: ['Client', 'Teacher', 'Student'] as UserRole[] },
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
  {
    path: 'tenants',
    canActivate: [roleGuard],
    data: { roles: ['Admin'] as UserRole[] },
    loadComponent: () => import('./admin/tenants/tenants').then((m) => m.Tenants),
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
