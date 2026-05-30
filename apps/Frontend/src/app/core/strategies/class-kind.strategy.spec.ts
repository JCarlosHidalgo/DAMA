import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import {
  AttendanceTarget,
  ClassFormPayload,
  ClassKind,
  ClassKindStrategies,
  ScheduledClassStrategy,
  UniqueClassStrategy,
} from './class-kind.strategy';
import { AttendanceApi } from '../api/attendance.api';
import { AttendanceRealtimeService } from '../api/attendance-realtime-service';
import { CourseApi } from '../api/course.api';
import { ScheduledClass, UniqueClass } from '../models/course.model';

function buildCourseApiSpy() {
  return {
    createScheduledClass: vi.fn(() => of({ id: 's-1' } as ScheduledClass)),
    updateScheduledClass: vi.fn(() => of(undefined as void)),
    deleteScheduledClass: vi.fn(() => of(undefined as void)),
    createUniqueClass: vi.fn(() => of({ id: 'u-1' } as UniqueClass)),
    updateUniqueClass: vi.fn(() => of(undefined as void)),
    deleteUniqueClass: vi.fn(() => of(undefined as void)),
  };
}

function buildAttendanceApiSpy() {
  return {
    markScheduled: vi.fn(() => of(undefined as void)),
    markUnique: vi.fn(() => of(undefined as void)),
    scheduledRoster: vi.fn(() => of([])),
    uniqueRoster: vi.fn(() => of([])),
  };
}

function buildRealtimeSpy() {
  return {
    connectToScheduled: vi.fn(() => of()),
    connectToUnique: vi.fn(() => of()),
  };
}

const FORM_PAYLOAD: ClassFormPayload = {
  courseId: 'course-1',
  teachers: [{ teacherId: 't-1', teacherName: 'Ana' }],
  startTime: '08:00',
  endTime: '09:00',
  dayOfWeekIndex: 2,
  date: '2026-05-10',
  maxStudentLimit: 25,
};

const ATTENDANCE_TARGET: AttendanceTarget = {
  classId: 'class-1',
  courseName: 'Yoga',
  classDate: '2026-05-10',
};

describe('class-kind strategies', () => {
  let courseApi: ReturnType<typeof buildCourseApiSpy>;
  let attendanceApi: ReturnType<typeof buildAttendanceApiSpy>;
  let realtime: ReturnType<typeof buildRealtimeSpy>;

  beforeEach(() => {
    courseApi = buildCourseApiSpy();
    attendanceApi = buildAttendanceApiSpy();
    realtime = buildRealtimeSpy();

    TestBed.configureTestingModule({
      providers: [
        { provide: CourseApi, useValue: courseApi },
        { provide: AttendanceApi, useValue: attendanceApi },
        { provide: AttendanceRealtimeService, useValue: realtime },
      ],
    });
  });

  describe('ScheduledClassStrategy', () => {
    let strategy: ScheduledClassStrategy;

    beforeEach(() => {
      strategy = TestBed.inject(ScheduledClassStrategy);
    });

    it('reports its kind as Scheduled', () => {
      expect(strategy.kind).toBe('Scheduled');
    });

    it('delegates create to CourseApi.createScheduledClass', () => {
      strategy.create(FORM_PAYLOAD).subscribe();

      expect(courseApi.createScheduledClass).toHaveBeenCalledWith({
        courseId: 'course-1',
        dayOfWeekIndex: 2,
        maxStudentLimit: 25,
        startTime: '08:00',
        endTime: '09:00',
        teachers: FORM_PAYLOAD.teachers,
      });
    });

    it('delegates update to CourseApi.updateScheduledClass with the class id', () => {
      strategy.update('class-7', FORM_PAYLOAD).subscribe();

      expect(courseApi.updateScheduledClass).toHaveBeenCalledWith('class-7', {
        dayOfWeekIndex: 2,
        maxStudentLimit: 25,
        startTime: '08:00',
        endTime: '09:00',
        teachers: FORM_PAYLOAD.teachers,
      });
    });

    it('delegates delete to CourseApi.deleteScheduledClass', () => {
      strategy.delete('class-7').subscribe();
      expect(courseApi.deleteScheduledClass).toHaveBeenCalledWith('class-7');
    });

    it('delegates markAttendance to AttendanceApi.markScheduled', () => {
      strategy.markAttendance({ classId: 'c', courseName: 'Yoga' }).subscribe();
      expect(attendanceApi.markScheduled).toHaveBeenCalledWith({
        classId: 'c',
        courseName: 'Yoga',
      });
    });

    it('delegates fetchRoster to AttendanceApi.scheduledRoster with classId + classDate', () => {
      strategy.fetchRoster(ATTENDANCE_TARGET).subscribe();
      expect(attendanceApi.scheduledRoster).toHaveBeenCalledWith('class-1', '2026-05-10');
    });

    it('delegates connectRealtime to AttendanceRealtimeService.connectToScheduled', () => {
      strategy.connectRealtime(ATTENDANCE_TARGET).subscribe();
      expect(realtime.connectToScheduled).toHaveBeenCalledWith('class-1', '2026-05-10');
    });
  });

  describe('UniqueClassStrategy', () => {
    let strategy: UniqueClassStrategy;

    beforeEach(() => {
      strategy = TestBed.inject(UniqueClassStrategy);
    });

    it('reports its kind as Unique', () => {
      expect(strategy.kind).toBe('Unique');
    });

    it('delegates create to CourseApi.createUniqueClass', () => {
      strategy.create(FORM_PAYLOAD).subscribe();

      expect(courseApi.createUniqueClass).toHaveBeenCalledWith({
        courseId: 'course-1',
        date: '2026-05-10',
        maxStudentLimit: 25,
        startTime: '08:00',
        endTime: '09:00',
        teachers: FORM_PAYLOAD.teachers,
      });
    });

    it('delegates update to CourseApi.updateUniqueClass with the class id', () => {
      strategy.update('class-9', FORM_PAYLOAD).subscribe();

      expect(courseApi.updateUniqueClass).toHaveBeenCalledWith('class-9', {
        date: '2026-05-10',
        maxStudentLimit: 25,
        startTime: '08:00',
        endTime: '09:00',
        teachers: FORM_PAYLOAD.teachers,
      });
    });

    it('delegates delete to CourseApi.deleteUniqueClass', () => {
      strategy.delete('class-9').subscribe();
      expect(courseApi.deleteUniqueClass).toHaveBeenCalledWith('class-9');
    });

    it('delegates markAttendance to AttendanceApi.markUnique', () => {
      strategy.markAttendance({ classId: 'c', courseName: 'Yoga' }).subscribe();
      expect(attendanceApi.markUnique).toHaveBeenCalledWith({
        classId: 'c',
        courseName: 'Yoga',
      });
    });

    it('delegates fetchRoster to AttendanceApi.uniqueRoster with classId only', () => {
      strategy.fetchRoster(ATTENDANCE_TARGET).subscribe();
      expect(attendanceApi.uniqueRoster).toHaveBeenCalledWith('class-1');
    });

    it('delegates connectRealtime to AttendanceRealtimeService.connectToUnique', () => {
      strategy.connectRealtime(ATTENDANCE_TARGET).subscribe();
      expect(realtime.connectToUnique).toHaveBeenCalledWith('class-1');
    });
  });

  describe('ClassKindStrategies dispatcher', () => {
    it('returns the ScheduledClassStrategy for kind=Scheduled', () => {
      const strategies = TestBed.inject(ClassKindStrategies);
      const dispatched = strategies.for('Scheduled');
      expect(dispatched.kind).toBe('Scheduled');
      expect(dispatched).toBe(TestBed.inject(ScheduledClassStrategy));
    });

    it('returns the UniqueClassStrategy for kind=Unique', () => {
      const strategies = TestBed.inject(ClassKindStrategies);
      const dispatched = strategies.for('Unique');
      expect(dispatched.kind).toBe('Unique');
      expect(dispatched).toBe(TestBed.inject(UniqueClassStrategy));
    });

    it.each(['Scheduled', 'Unique'] as ClassKind[])('round-trips kind=%s', (kind) => {
      const strategies = TestBed.inject(ClassKindStrategies);
      expect(strategies.for(kind).kind).toBe(kind);
    });
  });
});
