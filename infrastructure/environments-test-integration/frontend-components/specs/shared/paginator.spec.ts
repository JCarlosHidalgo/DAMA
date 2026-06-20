import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Paginator } from '@shared/components/paginator/paginator';

describe('Paginator', () => {
  it('no tiene violaciones en página intermedia', async () => {
    const { container } = await render(Paginator, {
      providers: [provideNoopAnimations()],
      inputs: { page: { currentIndex: 1, maxIndex: 3 } },
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones en primera página (botón Anterior deshabilitado)', async () => {
    const { container } = await render(Paginator, {
      providers: [provideNoopAnimations()],
      inputs: { page: { currentIndex: 0, maxIndex: 3 } },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
