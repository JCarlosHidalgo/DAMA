import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { MockProvider } from 'ng-mocks';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { Login } from '@pages/login/login';
import { AuthService } from '@core/auth/auth-service';
import { HttpErrorMapper } from '@core/services/http-error-mapper';

describe('Login', () => {
  const providers = [
    provideNoopAnimations(),
    provideRouter([]),
    MockProvider(AuthService),
    MockProvider(HttpErrorMapper, {
      mapError: vi.fn().mockReturnValue('Credenciales inválidas'),
    }),
  ];

  it('no tiene violaciones en estado inicial', async () => {
    const { container } = await render(Login, { providers });
    expect(await axe(container)).toHaveNoViolations();
  });
});
