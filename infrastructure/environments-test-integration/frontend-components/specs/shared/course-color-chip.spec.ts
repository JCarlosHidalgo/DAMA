import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { CourseColorChip } from '@shared/components/course-color-chip/course-color-chip';

describe('CourseColorChip', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(CourseColorChip, {
      inputs: { courseId: 'abc-123', name: 'Matemáticas' },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
