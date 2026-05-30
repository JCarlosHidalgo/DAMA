import { describe, it, expect } from 'vitest';

import { mapClaims } from './jwt.model';

describe('mapClaims', () => {
  it('maps all snake_case claims to camelCase fields', () => {
    const raw = {
      tenant_id: 'tenant-1',
      tenant_name: 'Tenant Uno',
      user_id: 'user-1',
      user_name: 'student@example.com',
      role: 'Teacher',
      tenant_timezone: 'America/Mexico_City',
      exp: 1_700_000_000,
    };

    const claims = mapClaims(raw);

    expect(claims).toEqual({
      tenantId: 'tenant-1',
      tenantName: 'Tenant Uno',
      userId: 'user-1',
      userName: 'student@example.com',
      role: 'Teacher',
      tenantTimezone: 'America/Mexico_City',
      exp: 1_700_000_000,
    });
  });

  it('falls back to empty strings and default role/timezone when claims are missing', () => {
    const claims = mapClaims({ exp: 0 });

    expect(claims.tenantId).toBe('');
    expect(claims.tenantName).toBe('');
    expect(claims.userId).toBe('');
    expect(claims.userName).toBe('');
    expect(claims.role).toBe('Student');
    expect(claims.tenantTimezone).toBe('America/La_Paz');
    expect(claims.exp).toBe(0);
  });

  it('coerces numeric exp from string form', () => {
    const claims = mapClaims({ exp: '1700000000' as unknown as number });

    expect(claims.exp).toBe(1_700_000_000);
  });

  it('falls back to exp=0 when raw exp is undefined', () => {
    const claims = mapClaims({ exp: undefined as unknown as number });

    expect(claims.exp).toBe(0);
  });
});
