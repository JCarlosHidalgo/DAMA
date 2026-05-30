import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { PageHead } from './page-head';

describe('PageHead', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<PageHead>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageHead],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(PageHead);
  });

  function render(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the title in the h1', () => {
    fixture.componentRef.setInput('title', 'Mi página');
    expect(render().querySelector('h1')?.textContent).toContain('Mi página');
  });

  it('does not render a subtitle when input is null', () => {
    fixture.componentRef.setInput('title', 'Mi página');
    expect(render().querySelector('p.t-small')).toBeNull();
  });

  it('renders the subtitle when provided', () => {
    fixture.componentRef.setInput('title', 'Mi página');
    fixture.componentRef.setInput('subtitle', 'Subtítulo');
    expect(render().querySelector('p.t-small')?.textContent).toContain('Subtítulo');
  });
});
