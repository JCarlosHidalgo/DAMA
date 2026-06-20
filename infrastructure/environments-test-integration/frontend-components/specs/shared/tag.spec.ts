import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { Tag } from '@shared/components/tag/tag';

describe('Tag', () => {
  const variants = ['neutral', 'primary', 'success', 'warning', 'danger'] as const;

  for (const variant of variants) {
    it(`no tiene violaciones — variante ${variant}`, async () => {
      const { container } = await render(
        `<app-tag variant="${variant}">Activo</app-tag>`,
        { imports: [Tag] },
      );
      expect(await axe(container)).toHaveNoViolations();
    });
  }
});
