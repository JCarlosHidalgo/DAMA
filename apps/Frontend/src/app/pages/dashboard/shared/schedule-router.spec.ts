import { Component, signal, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { ScheduleRouter } from './schedule-router';
import { Schedule } from '@pages/dashboard/client/schedule/schedule';
import { TeacherSchedule } from '@pages/dashboard/teacher/schedule/schedule';
import { StudentSchedule } from '@pages/dashboard/student/schedule/schedule';
import { Placeholder } from './placeholder';
import { AuthService, UserRole } from '@core/auth';

@Component({ selector: 'app-client-schedule', standalone: true, template: 'CLIENT_SCHEDULE' })
class StubClientSchedule {}

@Component({ selector: 'app-teacher-schedule', standalone: true, template: 'TEACHER_SCHEDULE' })
class StubTeacherSchedule {}

@Component({ selector: 'app-student-schedule', standalone: true, template: 'STUDENT_SCHEDULE' })
class StubStudentSchedule {}

@Component({ selector: 'app-placeholder', standalone: true, template: 'PLACEHOLDER' })
class StubPlaceholder {}

describe('ScheduleRouter', () => {
  const currentRole = signal<UserRole | null>(null);
  const authStub = { currentRole };

  async function instantiate(role: UserRole | null) {
    TestBed.resetTestingModule();
    currentRole.set(role);
    await TestBed.configureTestingModule({
      imports: [ScheduleRouter],
      providers: [provideZonelessChangeDetection(), { provide: AuthService, useValue: authStub }],
    })
      .overrideComponent(ScheduleRouter, {
        remove: { imports: [Schedule, TeacherSchedule, StudentSchedule, Placeholder] },
        add: {
          imports: [StubClientSchedule, StubTeacherSchedule, StubStudentSchedule, StubPlaceholder],
        },
      })
      .compileComponents();
    const fixture = TestBed.createComponent(ScheduleRouter);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => currentRole.set(null));

  it('renders the client schedule when role is Client', async () => {
    const fixture = await instantiate('Client');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('CLIENT_SCHEDULE');
  });

  it('renders the teacher schedule when role is Teacher', async () => {
    const fixture = await instantiate('Teacher');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('TEACHER_SCHEDULE');
  });

  it('renders the student schedule when role is Student', async () => {
    const fixture = await instantiate('Student');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('STUDENT_SCHEDULE');
  });

  it('falls back to the placeholder for other roles', async () => {
    const fixture = await instantiate('Admin');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('PLACEHOLDER');
  });
});
