import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { Icon } from '@shared/components/icon/icon';

describe('Icon', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(Icon, {
      inputs: { name: 'check' },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
