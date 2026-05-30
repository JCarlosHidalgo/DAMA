import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { Placeholder } from './placeholder';

describe('Placeholder', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Placeholder>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Placeholder],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(Placeholder);
  });

  it('renders the provided title and the en-construcción subtitle', () => {
    fixture.componentRef.setInput('title', 'Resumen');
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('mat-card-title')?.textContent).toContain('Resumen');
    expect(host.querySelector('mat-card-subtitle')?.textContent).toContain('En construcción');
  });
});
