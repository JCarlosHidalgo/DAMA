import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { LoadingSkeleton } from './loading-skeleton';

describe('LoadingSkeleton', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<LoadingSkeleton>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingSkeleton],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(LoadingSkeleton);
  });

  function renderedDiv(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('div');
  }

  it('renders with default height 16px and width 100%', () => {
    const div = renderedDiv();
    expect(div.style.height).toBe('16px');
    expect(div.style.width).toBe('100%');
  });

  it('applies the height input as pixels', () => {
    fixture.componentRef.setInput('height', 42);
    expect(renderedDiv().style.height).toBe('42px');
  });

  it('applies the width input as raw CSS string', () => {
    fixture.componentRef.setInput('width', '50%');
    expect(renderedDiv().style.width).toBe('50%');
  });
});
