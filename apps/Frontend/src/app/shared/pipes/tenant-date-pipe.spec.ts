import {
  ChangeDetectionStrategy,
  Component,
  signal,
  provideZonelessChangeDetection,
} from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { TenantDatePipe, TenantDatePrecision } from './tenant-date-pipe';
import { AuthService, SessionStorageTokenStorage } from '@core/auth';
import { InMemoryTokenStorage, buildJwtToken, buildJwtClaims } from '@testing';

@Component({
  selector: 'app-tenant-date-host',
  imports: [TenantDatePipe],
  template: `<span data-testid="formatted">{{ value() | tenantDate: precision() }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class TenantDateHost {
  readonly value = signal<string | null | undefined>(null);
  readonly precision = signal<TenantDatePrecision>('date');
}

function configureForTimezone(tenantTimezone: string): void {
  const token = buildJwtToken(buildJwtClaims({ tenantTimezone }));
  TestBed.configureTestingModule({
    imports: [TenantDateHost],
    providers: [
      provideZonelessChangeDetection(),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: SessionStorageTokenStorage, useValue: new InMemoryTokenStorage(token) },
    ],
  });
  TestBed.inject(AuthService);
}

function createPipe(): TenantDatePipe {
  return TestBed.runInInjectionContext(() => new TenantDatePipe());
}

describe('TenantDatePipe (direct instance)', () => {
  beforeEach(() => {
    sessionStorage.clear();
    configureForTimezone('America/La_Paz');
  });

  it('returns "—" for null', () => {
    expect(createPipe().transform(null)).toBe('—');
  });

  it('returns "—" for undefined', () => {
    expect(createPipe().transform(undefined)).toBe('—');
  });

  it('returns "—" for empty string', () => {
    expect(createPipe().transform('')).toBe('—');
  });

  it('formats an iso datetime as date-only by default', () => {
    const formatted = createPipe().transform('2026-04-15T18:30:00Z');
    expect(formatted).toMatch(/2026/);
    expect(formatted).toMatch(/15/);
    expect(formatted).not.toMatch(/:\d{2}/);
  });

  it('includes hours and minutes when precision is datetime', () => {
    const formatted = createPipe().transform('2026-04-15T18:30:00Z', 'datetime');
    expect(formatted).toMatch(/2026/);
    expect(formatted).toMatch(/\d{2}:\d{2}/);
  });

  it('reuses cached formatter for repeated calls with the same timezone+precision', () => {
    const pipe = createPipe();
    expect(pipe.transform('2026-04-15T18:30:00Z')).toBe(pipe.transform('2026-04-15T18:30:00Z'));
  });
});

describe('TenantDatePipe (via host component)', () => {
  let host: TenantDateHost;
  let fixture: ReturnType<typeof TestBed.createComponent<TenantDateHost>>;

  function renderedText(): string {
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('[data-testid="formatted"]')?.textContent ?? '';
  }

  function setUp(tenantTimezone: string): void {
    sessionStorage.clear();
    TestBed.resetTestingModule();
    configureForTimezone(tenantTimezone);
    fixture = TestBed.createComponent(TenantDateHost);
    host = fixture.componentInstance;
  }

  beforeEach(() => {
    setUp('America/La_Paz');
  });

  it('renders the em-dash placeholder when bound to null', () => {
    host.value.set(null);
    expect(renderedText()).toContain('—');
  });

  it('renders a date for a non-null iso input', () => {
    host.value.set('2026-04-15T18:30:00Z');
    expect(renderedText()).toMatch(/2026/);
  });

  it('formats in the UTC tenant timezone when configured for UTC', () => {
    setUp('UTC');
    host.value.set('2026-04-16T02:00:00Z');
    expect(renderedText()).toMatch(/16/);
  });

  it('formats in La Paz timezone, where late-evening UTC rolls back to the prior local day', () => {
    setUp('America/La_Paz');
    host.value.set('2026-04-16T02:00:00Z');
    expect(renderedText()).toMatch(/15/);
  });
});
