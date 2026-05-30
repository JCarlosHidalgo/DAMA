import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { TokenDecoder } from './token-decoder';
import { buildJwtClaims } from '../../../testing/builders/jwt-claims.builder';
import {
  MALFORMED_TOKEN,
  buildJwtToken,
} from '../../../testing/fixtures/jwt-tokens.fixture';

describe('TokenDecoder', () => {
  let decoder: TokenDecoder;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    decoder = TestBed.inject(TokenDecoder);
  });

  it('decodes a well-formed token into mapped claims', () => {
    const claims = buildJwtClaims({ userName: 'someone@example.com', role: 'Admin' });
    const token = buildJwtToken(claims);

    const decoded = decoder.decode(token);

    expect(decoded).not.toBeNull();
    expect(decoded?.userName).toBe('someone@example.com');
    expect(decoded?.role).toBe('Admin');
    expect(decoded?.tenantId).toBe(claims.tenantId);
    expect(decoded?.exp).toBe(claims.exp);
  });

  it('returns null when the token is malformed', () => {
    expect(decoder.decode(MALFORMED_TOKEN)).toBeNull();
  });

  it('returns null when the token payload is not valid JSON', () => {
    const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
    const garbledPayload = 'not-base64-json';
    const corrupted = `${header}.${garbledPayload}.signature`;

    expect(decoder.decode(corrupted)).toBeNull();
  });
});
