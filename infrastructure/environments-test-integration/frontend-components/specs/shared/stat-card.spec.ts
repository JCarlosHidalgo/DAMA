import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { StatCard } from '@shared/components/stat-card/stat-card';

describe('StatCard', () => {
  it('no tiene violaciones con delta', async () => {
    const { container } = await render(StatCard, {
      inputs: {
        label: 'Ingresos',
        value: '$1,200',
        delta: { sign: 'up' as const, value: '+12%' },
        sub: 'vs. mes anterior',
        icon: null,
      },
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones sin delta ni icono', async () => {
    const { container } = await render(StatCard, {
      inputs: { label: 'Alumnos', value: '42', delta: null, sub: null, icon: null },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
