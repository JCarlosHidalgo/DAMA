import { AbstractControl } from '@angular/forms';
import { describe, it, expect } from 'vitest';

import { noDotValidator } from './courses.validators';

function control(value: unknown): AbstractControl {
  return { value } as AbstractControl;
}

describe('noDotValidator', () => {
  it('flags a value containing a dot', () => {
    expect(noDotValidator(control('Yoga 2.0'))).toEqual({ hasDot: true });
  });

  it('passes a value without a dot', () => {
    expect(noDotValidator(control('Yoga'))).toBeNull();
  });

  it('passes a nullish value', () => {
    expect(noDotValidator(control(null))).toBeNull();
  });
});
