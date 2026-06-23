import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { ICON_REGISTRY, type IconName } from './icon-registry';

@Component({
  selector: 'app-icon',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<fa-icon [icon]="def()" [fixedWidth]="true" />`,
  host: { class: 'inline-flex leading-none' },
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly def = computed(() => ICON_REGISTRY[this.name()]);
}
