import { JwtClaims, UserRole } from '../../app/core/auth/jwt.model';

export function buildJwtClaims(overrides: Partial<JwtClaims> = {}): JwtClaims {
  const oneHourFromNowSeconds = Math.floor(Date.now() / 1000) + 60 * 60;
  return {
    tenantId: 'tenant-1',
    tenantName: 'Tenant Uno',
    userId: 'user-1',
    userName: 'student@example.com',
    role: 'Student',
    tenantTimezone: 'America/La_Paz',
    exp: oneHourFromNowSeconds,
    ...overrides,
  };
}

export function withRole(role: UserRole, overrides: Partial<JwtClaims> = {}): JwtClaims {
  return buildJwtClaims({ role, ...overrides });
}

export function withExpiry(expEpochSeconds: number, overrides: Partial<JwtClaims> = {}): JwtClaims {
  return buildJwtClaims({ exp: expEpochSeconds, ...overrides });
}
