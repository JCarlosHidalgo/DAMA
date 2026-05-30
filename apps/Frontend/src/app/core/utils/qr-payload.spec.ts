import { describe, it, expect } from 'vitest';

import { QrPayload, decodeQr, encodeQr } from './qr-payload';

describe('encodeQr / decodeQr', () => {
  const samplePayload: QrPayload = {
    tenantId: 'tenant-1',
    courseName: 'Matemáticas',
    kind: 'SCHEDULED',
    classId: 'class-42',
  };

  it('produces a string prefixed with dama1:', () => {
    expect(encodeQr(samplePayload).startsWith('dama1:')).toBe(true);
  });

  it('round-trips a SCHEDULED payload', () => {
    const encoded = encodeQr(samplePayload);
    expect(decodeQr(encoded)).toEqual(samplePayload);
  });

  it('round-trips a UNIQUE payload with non-ascii characters', () => {
    const unique: QrPayload = {
      tenantId: 'tenant-ñ',
      courseName: '日本語',
      kind: 'UNIQUE',
      classId: 'class-99',
    };
    expect(decodeQr(encodeQr(unique))).toEqual(unique);
  });

  describe('legacy dot-separated format', () => {
    it('decodes a 4-part SCHEDULED payload', () => {
      const legacy = 'tenant-7.Biología.SCHEDULED.class-7';
      expect(decodeQr(legacy)).toEqual({
        tenantId: 'tenant-7',
        courseName: 'Biología',
        kind: 'SCHEDULED',
        classId: 'class-7',
      });
    });

    it('decodes a 4-part UNIQUE payload', () => {
      expect(decodeQr('t.c.UNIQUE.id')).toEqual({
        tenantId: 't',
        courseName: 'c',
        kind: 'UNIQUE',
        classId: 'id',
      });
    });

    it('rejects legacy payloads with the wrong number of parts', () => {
      expect(decodeQr('only.two')).toBeNull();
      expect(decodeQr('a.b.c.d.e')).toBeNull();
    });

    it('rejects legacy payloads with an unknown kind', () => {
      expect(decodeQr('t.c.WEEKLY.id')).toBeNull();
    });
  });

  describe('error paths', () => {
    it('returns null when the dama1 body is not valid base64', () => {
      expect(decodeQr('dama1:not-valid-base64-!@#$%')).toBeNull();
    });

    it('returns null when the decoded JSON is missing fields', () => {
      const json = JSON.stringify({ tenantId: 'only' });
      const base64Url = btoa(json)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
      expect(decodeQr(`dama1:${base64Url}`)).toBeNull();
    });

    it('returns null when the kind is invalid in a well-formed JSON body', () => {
      const json = JSON.stringify({
        tenantId: 't',
        courseName: 'c',
        kind: 'INVALID',
        classId: 'id',
      });
      const base64Url = btoa(json)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
      expect(decodeQr(`dama1:${base64Url}`)).toBeNull();
    });
  });
});
