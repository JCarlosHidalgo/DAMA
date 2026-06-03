import { describe, expect, it } from 'vitest';

import { firstValidationMessage } from './validation-messages';

describe('firstValidationMessage', () => {
  it('returns null when there are no errors', () => {
    expect(firstValidationMessage(null)).toBeNull();
    expect(firstValidationMessage({})).toBeNull();
  });

  it('maps required to a mandatory-field message', () => {
    expect(firstValidationMessage({ required: true })).toContain('obligatorio');
  });

  it('maps minlength using the required length', () => {
    expect(firstValidationMessage({ minlength: { requiredLength: 5, actualLength: 2 } })).toBe(
      'Mínimo 5 caracteres.',
    );
  });

  it('maps maxlength using the required length', () => {
    expect(firstValidationMessage({ maxlength: { requiredLength: 128, actualLength: 200 } })).toBe(
      'Máximo 128 caracteres.',
    );
  });

  it('maps an app-specific key', () => {
    expect(firstValidationMessage({ hasDot: true })).toContain('puntos');
  });

  it('falls back for unknown keys', () => {
    expect(firstValidationMessage({ somethingUnknown: true })).toBe('Valor inválido.');
  });

  it('uses the first error key when several are present', () => {
    expect(firstValidationMessage({ required: true, minlength: { requiredLength: 5 } })).toContain(
      'obligatorio',
    );
  });
});
