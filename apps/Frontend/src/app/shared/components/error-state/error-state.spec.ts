import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { ErrorState } from './error-state';

describe('ErrorState', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<ErrorState>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorState],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ErrorState);
  });

  function render(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the message text', () => {
    fixture.componentRef.setInput('message', 'Algo salió mal');
    expect(render().querySelector('p')?.textContent).toContain('Algo salió mal');
  });

  it('renders an app-icon with the warning icon', () => {
    fixture.componentRef.setInput('message', 'm');
    expect(render().querySelector('app-icon')).not.toBeNull();
  });
});
