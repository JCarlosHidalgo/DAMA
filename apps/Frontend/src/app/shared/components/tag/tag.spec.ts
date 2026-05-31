import { ChangeDetectionStrategy, Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { Tag, TagVariant } from './tag';

@Component({
  imports: [Tag],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-tag [variant]="variant" [dot]="dot">Etiqueta</app-tag>`,
})
class TagHost {
  variant: TagVariant = 'neutral';
  dot = false;
}

describe('Tag', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<TagHost>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TagHost],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(TagHost);
  });

  function renderedSpan(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('.dama-tag');
  }

  it('renders the projected content', () => {
    expect(renderedSpan().textContent?.trim()).toContain('Etiqueta');
  });

  it('applies is-neutral class by default', () => {
    expect(renderedSpan().className).toContain('is-neutral');
  });

  it.each<TagVariant>(['primary', 'success', 'warning', 'danger'])(
    'applies is-%s class when variant is "%s"',
    (variant) => {
      fixture.componentInstance.variant = variant;
      expect(renderedSpan().className).toContain(`is-${variant}`);
    },
  );

  it('does not render a dot by default', () => {
    expect(renderedSpan().querySelector('.dot')).toBeNull();
  });

  it('renders a dot element when dot input is true', () => {
    fixture.componentInstance.dot = true;
    expect(renderedSpan().querySelector('.dot')).not.toBeNull();
  });
});
