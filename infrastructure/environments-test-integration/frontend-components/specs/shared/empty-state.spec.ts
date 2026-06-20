import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { EmptyState } from '@shared/components/empty-state/empty-state';

describe('EmptyState', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(EmptyState, {
      inputs: { message: 'Sin resultados' },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
