import { JwtClaims } from '@core/auth';
import { buildJwtClaims } from '@testing/builders/jwt-claims.builder';

type RawJwtClaimsPayload = Record<string, string | number>;

function toBase64Url(input: string): string {
  const utf8Bytes = new TextEncoder().encode(input);
  let binaryString = '';
  for (const byteValue of utf8Bytes) {
    binaryString += String.fromCharCode(byteValue);
  }
  return btoa(binaryString).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function projectClaimsToRawPayload(claims: JwtClaims): RawJwtClaimsPayload {
  return {
    tenant_id: claims.tenantId,
    tenant_name: claims.tenantName,
    user_id: claims.userId,
    user_name: claims.userName,
    role: claims.role,
    tenant_timezone: claims.tenantTimezone,
    exp: claims.exp,
  };
}

export function buildJwtToken(claims: JwtClaims = buildJwtClaims()): string {
  const header = toBase64Url(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const payload = toBase64Url(JSON.stringify(projectClaimsToRawPayload(claims)));
  return `${header}.${payload}.signature-placeholder`;
}

export function buildRawPayloadToken(payload: RawJwtClaimsPayload): string {
  const header = toBase64Url(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const encodedPayload = toBase64Url(JSON.stringify(payload));
  return `${header}.${encodedPayload}.signature-placeholder`;
}

export const MALFORMED_TOKEN = 'this-is-not-a-jwt';
