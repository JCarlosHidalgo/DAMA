import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { EmptyState } from './empty-state';

describe('EmptyState', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<EmptyState>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyState],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(EmptyState);
  });

  function render(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the message text', () => {
    fixture.componentRef.setInput('message', 'No hay resultados');
    expect(render().querySelector('p')?.textContent).toContain('No hay resultados');
  });

  it('renders an <app-icon> element with the default ban icon', () => {
    fixture.componentRef.setInput('message', 'm');
    expect(render().querySelector('app-icon')).not.toBeNull();
  });

  it('accepts a custom icon name', () => {
    fixture.componentRef.setInput('message', 'm');
    fixture.componentRef.setInput('icon', 'users');
    expect(render().querySelector('app-icon')).not.toBeNull();
  });
});
