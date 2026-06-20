import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { FormControl, Validators } from '@angular/forms';
import { FieldError } from '@shared/forms/field-error';

describe('FieldError', () => {
  it('no tiene violaciones sin error activo', async () => {
    const control = new FormControl('valor válido');
    const { container } = await render(FieldError, { inputs: { control } });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones con mensaje de error visible', async () => {
    const control = new FormControl('', [Validators.required]);
    control.markAsTouched();
    control.updateValueAndValidity();
    const { container } = await render(FieldError, { inputs: { control } });
    expect(await axe(container)).toHaveNoViolations();
  });
});
