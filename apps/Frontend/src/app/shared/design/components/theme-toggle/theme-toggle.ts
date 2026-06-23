import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

import { ThemeService } from '@core/services';

import { Icon } from '@shared/design/components/icon';

@Component({
  selector: 'app-theme-toggle',
  imports: [MatButtonModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      mat-icon-button
      type="button"
      (click)="themeService.toggle()"
      [attr.aria-label]="label()"
      [attr.aria-pressed]="themeService.isDark()"
    >
      <app-icon [name]="themeService.isDark() ? 'sun' : 'moon'" />
    </button>
  `,
  host: { class: 'inline-flex' },
})
export class ThemeToggle {
  protected readonly themeService = inject(ThemeService);
  protected readonly label = computed(() =>
    this.themeService.isDark() ? 'Activar tema claro' : 'Activar tema oscuro',
  );
}
