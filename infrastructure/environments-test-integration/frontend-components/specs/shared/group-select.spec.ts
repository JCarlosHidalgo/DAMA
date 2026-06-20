import { render } from '@testing-library/angular';
import { axe } from 'vitest-axe';
import { MockProvider } from 'ng-mocks';
import { of } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { QueryClient } from '@tanstack/query-core';
import { provideAngularQuery } from '@tanstack/angular-query-experimental';
import { GroupSelect } from '@shared/components/group-select/group-select';
import { CourseApi } from '@core/api/course.api';
import { DialogService } from '@core/services/dialog-service';
import { NotificationService } from '@core/services/notification-service';

describe('GroupSelect', () => {
  it('no tiene violaciones de accesibilidad', async () => {
    const { container } = await render(GroupSelect, {
      providers: [
        provideNoopAnimations(),
        provideAngularQuery(new QueryClient()),
        MockProvider(CourseApi, { getGroups: () => of([]), getTeacherGroups: () => of([]) }),
        MockProvider(DialogService),
        MockProvider(NotificationService),
      ],
      inputs: { editable: false, locked: false, selectedGroupId: '', source: 'tenant' as const },
    });
    expect(await axe(container)).toHaveNoViolations();
  });
});
