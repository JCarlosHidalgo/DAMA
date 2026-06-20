import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { ErrorState } from '@shared/components/error-state/error-state';

describe('ErrorState', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(ErrorState, {
      inputs: { message: 'Error al cargar datos' },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
