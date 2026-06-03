import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const noDotValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  control.value?.includes('.') ? { hasDot: true } : null;
