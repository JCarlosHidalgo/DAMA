import { UserRole } from '@core/auth';

export function defaultRouteForRole(role: UserRole): string {
  switch (role) {
    case 'Client':
      return '/yo/resumen';
    case 'Teacher':
      return '/yo/horario';
    case 'Student':
      return '/yo/horario';
    case 'Admin':
      return '/yo/tenants';
  }
}
