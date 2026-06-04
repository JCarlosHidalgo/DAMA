import {
  ChangeDetectionStrategy,
  Component,
  signal,
  provideZonelessChangeDetection,
} from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { MoneyPipe } from './money-pipe';

@Component({
  selector: 'app-money-host',
  imports: [MoneyPipe],
  template: `<span data-testid="amount">{{ amount() | money }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class MoneyHost {
  readonly amount = signal<number | null | undefined>(0);
}

describe('MoneyPipe (direct instance)', () => {
  const pipe = new MoneyPipe();

  it('returns "—" for null', () => {
    expect(pipe.transform(null)).toBe('—');
  });

  it('returns "—" for undefined', () => {
    expect(pipe.transform(undefined)).toBe('—');
  });

  it('preserves up to two decimal places', () => {
    expect(pipe.transform(12.345)).toMatch(/12[.,]3[45]/);
  });

  it('formats negatives with a sign', () => {
    const formatted = pipe.transform(-7.5);
    expect(formatted).toContain('7');
    expect(formatted).toMatch(/-|−|\(/);
  });

  it('defaults to BOB when no currency is given', () => {
    expect(pipe.transform(50)).toMatch(/Bs/);
  });

  it('honours an explicit ISO 4217 currency', () => {
    const formatted = pipe.transform(50, 'USD');
    expect(formatted).toMatch(/\$|USD/);
    expect(formatted).not.toMatch(/Bs/);
  });
});

describe('MoneyPipe (via host component)', () => {
  let host: MoneyHost;
  let fixture: ReturnType<typeof TestBed.createComponent<MoneyHost>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MoneyHost],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(MoneyHost);
    host = fixture.componentInstance;
  });

  function renderedText(): string {
    fixture.detectChanges();
    const span = fixture.nativeElement.querySelector('[data-testid="amount"]') as HTMLElement;
    return span.textContent ?? '';
  }

  it('renders an em-dash placeholder for null', () => {
    host.amount.set(null);
    expect(renderedText()).toContain('—');
  });

  it('renders zero with the currency formatting', () => {
    host.amount.set(0);
    expect(renderedText()).toMatch(/0/);
  });

  it('renders a positive value', () => {
    host.amount.set(125);
    expect(renderedText()).toMatch(/125/);
  });
});
