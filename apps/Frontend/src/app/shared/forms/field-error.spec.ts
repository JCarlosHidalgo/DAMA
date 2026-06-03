import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';

import { FieldError } from './field-error';

describe('FieldError', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('shows the first validation message for an invalid control', async () => {
    const control = new FormControl('', { nonNullable: true, validators: Validators.required });
    const fixture = TestBed.createComponent(FieldError);
    fixture.componentRef.setInput('control', control);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('obligatorio');
  });

  it('clears the message once the control becomes valid', async () => {
    const control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(5)],
    });
    const fixture = TestBed.createComponent(FieldError);
    fixture.componentRef.setInput('control', control);
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent?.trim().length).toBeGreaterThan(0);

    control.setValue('valid value');
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });
});
