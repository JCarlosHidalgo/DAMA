import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { QrCard } from '@shared/components/qr-card/qr-card';

describe('QrCard', () => {
  it('no tiene violaciones con título y subtítulo', async () => {
    const { container } = await render(QrCard, {
      inputs: { payload: 'https://example.com', title: 'Código QR', subtitle: 'Escanear', size: 200 },
    });
    expect(await axe(container)).toHaveNoViolations();
  });

  it('no tiene violaciones sin título ni subtítulo', async () => {
    const { container } = await render(QrCard, {
      inputs: { payload: 'https://example.com', title: null, subtitle: null, size: 200 },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
