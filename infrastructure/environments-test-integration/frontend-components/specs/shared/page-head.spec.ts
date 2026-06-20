import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { PageHead } from '@shared/components/page-head/page-head';

describe('PageHead', () => {
  it('no tiene violaciones con subtítulo', async () => {
    const { container } = await render(PageHead, {
      inputs: { title: 'Cursos', subtitle: 'Gestiona tus cursos' },
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones sin subtítulo', async () => {
    const { container } = await render(PageHead, {
      inputs: { title: 'Cursos', subtitle: null },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
