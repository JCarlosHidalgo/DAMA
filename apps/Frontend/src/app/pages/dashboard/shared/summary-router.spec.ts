import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { SummaryRouter } from './summary-router';
import { ClientSummary } from '../client/summary/summary';
import { StudentSummary } from '../student/summary/summary';
import { Placeholder } from './placeholder';
import { AuthService } from '../../../core/auth/auth-service';
import { UserRole } from '../../../core/auth/jwt.model';

@Component({ selector: 'app-client-summary', standalone: true, template: 'CLIENT_SUMMARY' })
class StubClientSummary {}

@Component({ selector: 'app-student-summary', standalone: true, template: 'STUDENT_SUMMARY' })
class StubStudentSummary {}

@Component({ selector: 'app-placeholder', standalone: true, template: 'PLACEHOLDER' })
class StubPlaceholder {}

describe('SummaryRouter', () => {
  const currentRole = signal<UserRole | null>(null);
  const authStub = { currentRole };

  async function instantiate(role: UserRole | null) {
    TestBed.resetTestingModule();
    currentRole.set(role);
    await TestBed.configureTestingModule({
      imports: [SummaryRouter],
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthService, useValue: authStub },
      ],
    })
      .overrideComponent(SummaryRouter, {
        remove: { imports: [ClientSummary, StudentSummary, Placeholder] },
        add: { imports: [StubClientSummary, StubStudentSummary, StubPlaceholder] },
      })
      .compileComponents();
    const fixture = TestBed.createComponent(SummaryRouter);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => currentRole.set(null));

  it('renders the client summary for role=Client', async () => {
    const fixture = await instantiate('Client');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('CLIENT_SUMMARY');
  });

  it('renders the student summary for role=Student', async () => {
    const fixture = await instantiate('Student');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('STUDENT_SUMMARY');
  });

  it('falls back to the placeholder for other roles', async () => {
    const fixture = await instantiate('Teacher');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('PLACEHOLDER');
  });
});
