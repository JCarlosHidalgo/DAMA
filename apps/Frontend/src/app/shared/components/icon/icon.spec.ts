import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { Icon } from './icon';
import { ICON_REGISTRY, IconName } from './icon-registry';

describe('Icon', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Icon>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Icon],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(Icon);
  });

  it('resolves the IconDefinition from the registry for the given name', () => {
    fixture.componentRef.setInput('name', 'gauge');
    fixture.detectChanges();
    expect(fixture.componentInstance.def()).toBe(ICON_REGISTRY['gauge']);
  });

  it.each<IconName>(['users', 'qr', 'check', 'warning', 'chevron-left', 'chevron-right'])(
    'resolves "%s" without throwing',
    (name) => {
      fixture.componentRef.setInput('name', name);
      fixture.detectChanges();
      expect(fixture.componentInstance.def()).toBeDefined();
    },
  );

  it('renders an <fa-icon> element', () => {
    fixture.componentRef.setInput('name', 'plus');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('fa-icon')).not.toBeNull();
  });
});

describe('ICON_REGISTRY', () => {
  it('exposes a defined IconDefinition for every IconName', () => {
    const names: IconName[] = [
      'graduation-cap', 'users', 'calendar', 'qr', 'eye', 'eye-off', 'check',
      'warning', 'ban', 'edit', 'trash', 'plus', 'user-plus', 'logout', 'bars',
      'chevron-left', 'chevron-right', 'image', 'gauge', 'credit-card', 'receipt',
      'money-bill', 'chalkboard', 'calendar-check',
    ];
    for (const name of names) {
      expect(ICON_REGISTRY[name]).toBeDefined();
      expect(ICON_REGISTRY[name].icon).toBeDefined();
    }
  });
});
