import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const APP_KEY_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

export const appKeyValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (!value) {
    return null;
  }
  return APP_KEY_PATTERN.test(value) ? null : { appKey: true };
};
