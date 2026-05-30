export type UserRole = 'Admin' | 'Client' | 'Teacher' | 'Student';

export interface JwtClaims {
  tenantId: string;
  tenantName: string;
  userId: string;
  userName: string;
  role: UserRole;
  tenantTimezone: string;
  exp: number;
}

interface RawJwtPayload {
  [key: string]: string | number | undefined;
  exp: number;
}

const TENANT_ID_CLAIM = 'tenant_id';
const TENANT_NAME_CLAIM = 'tenant_name';
const USER_ID_CLAIM = 'user_id';
const USER_NAME_CLAIM = 'user_name';
const ROLE_CLAIM = 'role';
const TENANT_TIMEZONE_CLAIM = 'tenant_timezone';

export function mapClaims(raw: RawJwtPayload): JwtClaims {
  return {
    tenantId: String(raw[TENANT_ID_CLAIM] ?? ''),
    tenantName: String(raw[TENANT_NAME_CLAIM] ?? ''),
    userId: String(raw[USER_ID_CLAIM] ?? ''),
    userName: String(raw[USER_NAME_CLAIM] ?? ''),
    role: String(raw[ROLE_CLAIM] ?? 'Student') as UserRole,
    tenantTimezone: String(raw[TENANT_TIMEZONE_CLAIM] ?? 'America/La_Paz'),
    exp: Number(raw.exp ?? 0),
  };
}
