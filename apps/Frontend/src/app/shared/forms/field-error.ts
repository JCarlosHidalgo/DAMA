import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { startWith, switchMap } from 'rxjs';

import { firstValidationMessage } from './validation-messages';

@Component({
  selector: 'app-field-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `@if (message(); as text) {
    <span class="t-small text-danger">{{ text }}</span>
  }`,
  host: { class: 'block' },
})
export class FieldError {
  readonly control = input.required<AbstractControl>();

  private readonly controlEvents = toSignal(
    toObservable(this.control).pipe(switchMap((control) => control.events.pipe(startWith(null)))),
  );

  protected readonly message = computed(() => {
    this.controlEvents();
    return firstValidationMessage(this.control().errors);
  });
}
