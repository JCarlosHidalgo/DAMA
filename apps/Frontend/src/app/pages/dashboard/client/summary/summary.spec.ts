import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { describe, it, expect } from 'vitest';

import { ClientSummary } from './summary';
import { PaymentApi } from '@core/api';
import { PaymentSummary } from '@core/models';

const samplePayment: PaymentSummary = {
  totalEarnings: 1000,
  monthEarnings: 200,
  currency: 'BOB',
  firstPaymentDate: '2026-01-01T00:00:00Z',
  from: '2026-04-01',
  to: '2026-04-30',
};

describe('ClientSummary', () => {
  async function instantiate(api: { getSummary: () => unknown }) {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ClientSummary],
      providers: [provideZonelessChangeDetection(), { provide: PaymentApi, useValue: api }],
    }).compileComponents();
    return TestBed.createComponent(ClientSummary);
  }

  it('starts in loading state', async () => {
    const fixture = await instantiate({ getSummary: () => new Subject() });
    fixture.detectChanges();
    expect(fixture.componentInstance.state()).toEqual({ kind: 'loading' });
    expect(fixture.componentInstance.ready()).toBeNull();
  });

  it('transitions to ready and exposes the summary data', async () => {
    const fixture = await instantiate({ getSummary: () => of(samplePayment) });
    fixture.detectChanges();
    expect(fixture.componentInstance.state().kind).toBe('ready');
    expect(fixture.componentInstance.ready()).toEqual(samplePayment);
  });

  it('transitions to error when the request fails', async () => {
    const fixture = await instantiate({
      getSummary: () => throwError(() => new Error('boom')),
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.state().kind).toBe('error');
    expect(fixture.componentInstance.ready()).toBeNull();
  });
});
