import { CreateDebtTemplatePayload } from '@core/models';

export interface TemplateDialogResult {
  description: string;
  classQuantity: number;
  cost: number;
}

export type TemplateSubmitOutcome =
  | { kind: 'skip' }
  | { kind: 'submit'; payload: CreateDebtTemplatePayload };

export function debtTemplatesSubtitle(count: number): string {
  return `${count} plantilla(s)`;
}

export function resolveTemplateSubmit(
  result: TemplateDialogResult | undefined,
): TemplateSubmitOutcome {
  if (!result) {
    return { kind: 'skip' };
  }
  return {
    kind: 'submit',
    payload: {
      description: result.description,
      classQuantity: result.classQuantity,
      cost: result.cost,
    },
  };
}

export function deleteTemplateConfirmMessage(description: string): string {
  return `¿Eliminar plantilla "${description}"?`;
}
