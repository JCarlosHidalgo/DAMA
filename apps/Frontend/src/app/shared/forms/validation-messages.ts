import { ValidationErrors } from '@angular/forms';

export type ValidationMessageFactory = (error: unknown) => string;

const FALLBACK_MESSAGE = 'Valor inválido.';

export const VALIDATION_MESSAGES: Record<string, ValidationMessageFactory> = {
  required: () => 'Este campo es obligatorio.',
  minlength: (error) => {
    const requiredLength = (error as { requiredLength?: number }).requiredLength;
    return requiredLength ? `Mínimo ${requiredLength} caracteres.` : 'Valor demasiado corto.';
  },
  maxlength: (error) => {
    const requiredLength = (error as { requiredLength?: number }).requiredLength;
    return requiredLength ? `Máximo ${requiredLength} caracteres.` : 'Valor demasiado largo.';
  },
  min: (error) => {
    const min = (error as { min?: number }).min;
    return min != null ? `Valor mínimo: ${min}.` : 'Valor demasiado bajo.';
  },
  max: (error) => {
    const max = (error as { max?: number }).max;
    return max != null ? `Valor máximo: ${max}.` : 'Valor demasiado alto.';
  },
  email: () => 'Correo electrónico inválido.',
  pattern: () => 'El formato no es válido.',
  hasDot: () => 'No se permiten puntos (.).',
};

export function firstValidationMessage(errors: ValidationErrors | null): string | null {
  if (!errors) {
    return null;
  }
  const keys = Object.keys(errors);
  if (keys.length === 0) {
    return null;
  }
  const key = keys[0];
  const factory = VALIDATION_MESSAGES[key];
  return factory ? factory(errors[key]) : FALLBACK_MESSAGE;
}
