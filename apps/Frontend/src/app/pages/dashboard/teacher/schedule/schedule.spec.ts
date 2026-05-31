import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { TeacherSchedule } from './schedule';
import { CourseApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationService } from '@core/services';
import { Course, CourseScheduleEntry } from '@core/models';

const EMPTY_SCHEDULE = { scheduledClasses: [], uniqueClasses: [] };

function course(id: string, name: string): Course {
  return { id, name, tenantId: 'tenant-1' };
}

describe('TeacherSchedule', () => {
  let courseApi: {
    getTeacherSchedule: ReturnType<typeof vi.fn>;
    getCourse: ReturnType<typeof vi.fn>;
  };
  let matDialog: { open: ReturnType<typeof vi.fn> };
  let notifications: { error: ReturnType<typeof vi.fn> };

  async function setUp() {
    TestBed.resetTestingModule();
    courseApi = {
      getTeacherSchedule: vi.fn(() => of(EMPTY_SCHEDULE)),
      getCourse: vi.fn((id: string) => of(course(id, `Course ${id}`))),
    };
    matDialog = { open: vi.fn() };
    notifications = { error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [TeacherSchedule],
      providers: [
        provideZonelessChangeDetection(),
        { provide: CourseApi, useValue: courseApi },
        { provide: MatDialog, useValue: matDialog },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: { tenantTimezone: signal('America/La_Paz') } },
      ],
    }).compileComponents();

    return TestBed.createComponent(TeacherSchedule);
  }

  async function flush(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
  }

  it('loads the teacher schedule on construction', async () => {
    await setUp();
    await flush();
    expect(courseApi.getTeacherSchedule).toHaveBeenCalledWith(0);
  });

  it('shows an error and clears entries when the schedule fails to load', async () => {
    const fixture = await setUp();
    courseApi.getTeacherSchedule.mockReturnValue(throwError(() => new Error('down')));

    await fixture.componentInstance['onWeekDelta'](0);

    expect(notifications.error).toHaveBeenCalledWith('Error al cargar horario.');
    expect(fixture.componentInstance['entries']()).toEqual([]);
  });

  describe('week navigation', () => {
    it('accumulates the week delta', async () => {
      const fixture = await setUp();
      await flush();

      await fixture.componentInstance['onWeekDelta'](2);
      expect(courseApi.getTeacherSchedule).toHaveBeenLastCalledWith(2);

      await fixture.componentInstance['onWeekDelta'](3);
      expect(courseApi.getTeacherSchedule).toHaveBeenLastCalledWith(5);
    });

    it('resets to the current week on a zero delta', async () => {
      const fixture = await setUp();
      await flush();
      await fixture.componentInstance['onWeekDelta'](4);

      await fixture.componentInstance['onWeekDelta'](0);

      expect(courseApi.getTeacherSchedule).toHaveBeenLastCalledWith(0);
    });
  });

  describe('ensureCourseNames', () => {
    it('fetches and merges names for unknown course ids only', async () => {
      const fixture = await setUp();
      await flush();

      await fixture.componentInstance['ensureCourseNames']({
        scheduledClasses: [{ courseId: 'c1' }],
        uniqueClasses: [{ courseId: 'c2' }],
      });

      expect(courseApi.getCourse).toHaveBeenCalledWith('c1');
      expect(courseApi.getCourse).toHaveBeenCalledWith('c2');
      const ids = fixture.componentInstance['courses']().map((entry) => entry.id);
      expect(ids).toEqual(expect.arrayContaining(['c1', 'c2']));
    });

    it('does not fetch course ids that are already known', async () => {
      const fixture = await setUp();
      await flush();
      fixture.componentInstance['courses'].set([course('c1', 'Known')]);

      await fixture.componentInstance['ensureCourseNames']({
        scheduledClasses: [{ courseId: 'c1' }],
        uniqueClasses: [],
      });

      expect(courseApi.getCourse).not.toHaveBeenCalled();
    });

    it('ignores a course whose individual fetch fails', async () => {
      const fixture = await setUp();
      await flush();
      courseApi.getCourse.mockImplementation((id: string) =>
        id === 'bad' ? throwError(() => new Error('404')) : of(course(id, 'ok')),
      );

      await fixture.componentInstance['ensureCourseNames']({
        scheduledClasses: [{ courseId: 'good' }, { courseId: 'bad' }],
        uniqueClasses: [],
      });

      const ids = fixture.componentInstance['courses']().map((entry) => entry.id);
      expect(ids).toContain('good');
      expect(ids).not.toContain('bad');
    });
  });

  it('opens the attendance QR dialog for the clicked class', async () => {
    const fixture = await setUp();
    await flush();
    const entry = { classId: 'class-1', courseName: 'Yoga' } as CourseScheduleEntry;

    fixture.componentInstance.onEvent(entry);

    expect(matDialog.open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ data: { entry } }),
    );
  });
});
