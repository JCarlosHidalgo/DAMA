import { AbstractControl } from '@angular/forms';
import { describe, it, expect } from 'vitest';

import { appKeyValidator } from './configuration.validators';

function control(value: unknown): AbstractControl {
  return { value } as AbstractControl;
}

describe('appKeyValidator', () => {
  it('returns null for an empty value', () => {
    expect(appKeyValidator(control(''))).toBeNull();
  });

  it('returns null for a valid lowercase GUID', () => {
    expect(appKeyValidator(control('0190a1b2-c3d4-4e5f-8a9b-0c1d2e3f4a5b'))).toBeNull();
  });

  it('returns { appKey: true } for an invalid value', () => {
    expect(appKeyValidator(control('not-a-guid'))).toEqual({ appKey: true });
  });
});
