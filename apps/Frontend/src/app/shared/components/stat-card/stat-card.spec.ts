import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { StatCard } from './stat-card';

describe('StatCard', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<StatCard>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatCard],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(StatCard);
  });

  function render(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders label and value', () => {
    fixture.componentRef.setInput('label', 'Estudiantes');
    fixture.componentRef.setInput('value', '142');
    const host = render();
    expect(host.querySelector('.t-label-up')?.textContent).toContain('Estudiantes');
    expect(host.querySelector('.value')?.textContent).toContain('142');
  });

  it('does not render the icon container when icon input is null', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    expect(render().querySelector('app-icon')).toBeNull();
  });

  it('renders an app-icon when icon input is provided', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    fixture.componentRef.setInput('icon', 'gauge');
    expect(render().querySelector('app-icon')).not.toBeNull();
  });

  it('does not render the delta block when delta input is null', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    expect(render().querySelector('.delta')).toBeNull();
  });

  it('renders the delta value with is-up class for sign=up', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    fixture.componentRef.setInput('delta', { sign: 'up', value: '+12%' });
    const delta = render().querySelector('.delta') as HTMLElement;
    expect(delta.textContent).toContain('+12%');
    expect(delta.classList.contains('is-up')).toBe(true);
    expect(delta.classList.contains('is-down')).toBe(false);
  });

  it('renders the delta with is-down class for sign=down', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    fixture.componentRef.setInput('delta', { sign: 'down', value: '-3%' });
    const delta = render().querySelector('.delta') as HTMLElement;
    expect(delta.classList.contains('is-down')).toBe(true);
    expect(delta.classList.contains('is-up')).toBe(false);
  });

  it('does not render sub when input is null', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    expect(render().querySelector('.sub')).toBeNull();
  });

  it('renders sub text when provided', () => {
    fixture.componentRef.setInput('label', 'l');
    fixture.componentRef.setInput('value', 'v');
    fixture.componentRef.setInput('sub', 'últimos 30 días');
    expect(render().querySelector('.sub')?.textContent).toContain('últimos 30 días');
  });
});
