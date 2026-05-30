export type ClassKind = 'SCHEDULED' | 'UNIQUE';

export interface QrPayload {
  tenantId: string;
  courseName: string;
  kind: ClassKind;
  classId: string;
}

const PAYLOAD_PREFIX = 'dama1:';

export function encodeQr(payload: QrPayload): string {
  const json = JSON.stringify({
    tenantId: payload.tenantId,
    courseName: payload.courseName,
    kind: payload.kind,
    classId: payload.classId,
  });
  return PAYLOAD_PREFIX + toBase64Url(json);
}

export function decodeQr(rawQrText: string): QrPayload | null {
  if (!rawQrText.startsWith(PAYLOAD_PREFIX)) {
    return tryDecodeLegacy(rawQrText);
  }
  try {
    const json = fromBase64Url(rawQrText.slice(PAYLOAD_PREFIX.length));
    const parsed = JSON.parse(json) as Partial<QrPayload>;
    if (!isValidPayload(parsed)) {
      return null;
    }
    return {
      tenantId: parsed.tenantId,
      courseName: parsed.courseName,
      kind: parsed.kind,
      classId: parsed.classId,
    };
  } catch {
    return null;
  }
}

function tryDecodeLegacy(rawQrText: string): QrPayload | null {
  const pieces = rawQrText.split('.');
  if (pieces.length !== 4) {
    return null;
  }
  const [tenantId, courseName, kind, classId] = pieces;
  if (kind !== 'SCHEDULED' && kind !== 'UNIQUE') {
    return null;
  }
  return { tenantId, courseName, kind, classId };
}

function isValidPayload(candidate: Partial<QrPayload>): candidate is QrPayload {
  return (
    typeof candidate.tenantId === 'string' &&
    typeof candidate.courseName === 'string' &&
    typeof candidate.classId === 'string' &&
    (candidate.kind === 'SCHEDULED' || candidate.kind === 'UNIQUE')
  );
}

function toBase64Url(text: string): string {
  const utf8Bytes = new TextEncoder().encode(text);
  let binaryString = '';
  for (const byteValue of utf8Bytes) {
    binaryString += String.fromCharCode(byteValue);
  }
  return btoa(binaryString).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fromBase64Url(base64UrlText: string): string {
  const paddingNeeded = (4 - (base64UrlText.length % 4)) % 4;
  const standardBase64 =
    base64UrlText.replace(/-/g, '+').replace(/_/g, '/') + '='.repeat(paddingNeeded);
  const binaryString = atob(standardBase64);
  const utf8Bytes = new Uint8Array(binaryString.length);
  for (let byteIndex = 0; byteIndex < binaryString.length; byteIndex++) {
    utf8Bytes[byteIndex] = binaryString.charCodeAt(byteIndex);
  }
  return new TextDecoder().decode(utf8Bytes);
}
