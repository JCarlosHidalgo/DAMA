import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { LoadingSkeleton } from '@shared/components/loading-skeleton/loading-skeleton';

describe('LoadingSkeleton', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(LoadingSkeleton, {
      inputs: { height: 20, width: '100%' },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
