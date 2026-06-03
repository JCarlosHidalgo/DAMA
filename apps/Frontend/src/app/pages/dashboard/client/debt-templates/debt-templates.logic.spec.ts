import { describe, it, expect } from 'vitest';

import {
  debtTemplatesSubtitle,
  resolveTemplateSubmit,
  deleteTemplateConfirmMessage,
} from './debt-templates.logic';

describe('debtTemplatesSubtitle', () => {
  it('renders the count label', () => {
    expect(debtTemplatesSubtitle(2)).toBe('2 plantilla(s)');
  });
});

describe('resolveTemplateSubmit', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveTemplateSubmit(undefined)).toEqual({ kind: 'skip' });
  });

  it('submits with the dialog result as payload', () => {
    expect(resolveTemplateSubmit({ description: 'X', classQuantity: 3, cost: 50 })).toEqual({
      kind: 'submit',
      payload: { description: 'X', classQuantity: 3, cost: 50 },
    });
  });
});

describe('deleteTemplateConfirmMessage', () => {
  it('returns the confirm message with the template description in quotes', () => {
    expect(deleteTemplateConfirmMessage('Mensual')).toBe('¿Eliminar plantilla "Mensual"?');
  });
});
