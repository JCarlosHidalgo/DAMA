import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { describe, it, expect } from 'vitest';

import { StudentSummary } from './summary';
import { AttendanceApi } from '@core/api';
import { AuthService } from '@core/auth';
import { StudentRemainClasses } from '@core/models';
import { buildJwtClaims } from '@testing';

const sampleRemain: StudentRemainClasses = {
  tenantId: 'tenant-1',
  id: 'remain-1',
  numberOfClasses: 5,
  studentName: 'Alice',
};

describe('StudentSummary', () => {
  async function instantiate(api: { getMyRemain: () => unknown }) {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [StudentSummary],
      providers: [
        provideZonelessChangeDetection(),
        { provide: AttendanceApi, useValue: api },
        {
          provide: AuthService,
          useValue: {
            tenantTimezone: signal('America/La_Paz'),
            claims: signal(buildJwtClaims()),
          },
        },
      ],
    }).compileComponents();
    return TestBed.createComponent(StudentSummary);
  }

  it('starts in loading state', async () => {
    const fixture = await instantiate({ getMyRemain: () => new Subject() });
    fixture.detectChanges();
    expect(fixture.componentInstance.state()).toEqual({ kind: 'loading' });
  });

  it('transitions to ready and exposes the remain data', async () => {
    const fixture = await instantiate({ getMyRemain: () => of(sampleRemain) });
    fixture.detectChanges();
    expect(fixture.componentInstance.state().kind).toBe('ready');
    expect(fixture.componentInstance.ready()).toEqual(sampleRemain);
  });

  it('transitions to error when the request fails', async () => {
    const fixture = await instantiate({
      getMyRemain: () => throwError(() => new Error('boom')),
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.state().kind).toBe('error');
    expect(fixture.componentInstance.ready()).toBeNull();
  });

  it('exposes ready=null when state is not ready', async () => {
    const fixture = await instantiate({ getMyRemain: () => new Subject() });
    fixture.detectChanges();
    expect(fixture.componentInstance.ready()).toBeNull();
  });
});
