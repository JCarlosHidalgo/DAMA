import { describe, it, expect } from 'vitest';

import { normalizeSchedule, weekAnchorIsoDate } from './schedule-normalize';
import {
  Course,
  GetCourseScheduleDTO,
  GetScheduledClassDTO,
  GetUniqueClassDTO,
} from '@core/models';

const COURSES: Course[] = [
  { id: 'course-1', name: 'Yoga', tenantId: 't' },
  { id: 'course-2', name: 'Pilates', tenantId: 't' },
];

function buildScheduledClass(overrides: Partial<GetScheduledClassDTO> = {}): GetScheduledClassDTO {
  return {
    id: 'sched-1',
    courseId: 'course-1',
    dayOfWeekIndex: 1,
    maxStudentLimit: 0,
    startTime: '08:00',
    endTime: '09:00',
    teachers: [],
    groupId: 'group-1',
    groupName: 'Grupo 1',
    ...overrides,
  };
}

function buildUniqueClass(overrides: Partial<GetUniqueClassDTO> = {}): GetUniqueClassDTO {
  return {
    id: 'unique-1',
    courseId: 'course-2',
    date: '2026-04-10',
    maxStudentLimit: 0,
    startTime: '10:00',
    endTime: '11:00',
    teachers: [],
    groupId: 'group-2',
    groupName: 'Grupo 2',
    ...overrides,
  };
}

describe('normalizeSchedule', () => {
  const monday = new Date(2026, 3, 6);

  it('returns an empty list when there are no classes', () => {
    const response: GetCourseScheduleDTO = { scheduledClasses: [], uniqueClasses: [] };

    expect(normalizeSchedule(response, 0, COURSES, monday)).toEqual([]);
  });

  it('handles missing arrays as if they were empty', () => {
    const response = {} as unknown as GetCourseScheduleDTO;

    expect(normalizeSchedule(response, 0, COURSES, monday)).toEqual([]);
  });

  it('maps scheduled classes to the correct occurrence date in the current ISO week', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [
        buildScheduledClass({ dayOfWeekIndex: 1 }),
        buildScheduledClass({ id: 'sched-2', dayOfWeekIndex: 5 }),
      ],
      uniqueClasses: [],
    };

    const entries = normalizeSchedule(response, 0, COURSES, monday);

    expect(entries).toHaveLength(2);
    expect(entries[0].date).toBe('2026-04-06');
    expect(entries[1].date).toBe('2026-04-10');
    expect(entries[0].classKind).toBe('Scheduled');
    expect(entries[0].courseName).toBe('Yoga');
  });

  it('shifts the occurrence date by weekIndex weeks', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 1 })],
      uniqueClasses: [],
    };

    expect(normalizeSchedule(response, 1, COURSES, monday)[0].date).toBe('2026-04-13');
    expect(normalizeSchedule(response, -1, COURSES, monday)[0].date).toBe('2026-03-30');
  });

  it('aligns the week start to Monday when invoked on a Sunday', () => {
    const sunday = new Date(2026, 3, 12);
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 1 })],
      uniqueClasses: [],
    };

    expect(normalizeSchedule(response, 0, COURSES, sunday)[0].date).toBe('2026-04-06');
  });

  it('maps unique classes preserving their date and merging with scheduled', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 2 })],
      uniqueClasses: [buildUniqueClass({ date: '2026-04-10' })],
    };

    const entries = normalizeSchedule(response, 0, COURSES, monday);

    expect(entries).toHaveLength(2);
    expect(entries[1].classKind).toBe('Unique');
    expect(entries[1].date).toBe('2026-04-10');
    expect(entries[1].courseName).toBe('Pilates');
    expect(entries[1].dayOfWeekIndex).toBeUndefined();
  });

  it('falls back to "—" when the course is unknown', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ courseId: 'missing' })],
      uniqueClasses: [buildUniqueClass({ courseId: 'also-missing' })],
    };

    const entries = normalizeSchedule(response, 0, COURSES, monday);
    expect(entries[0].courseName).toBe('—');
    expect(entries[1].courseName).toBe('—');
  });

  it('uses empty teachers array when teachers are missing from a scheduled class', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ teachers: undefined as unknown as never })],
      uniqueClasses: [buildUniqueClass({ teachers: undefined as unknown as never })],
    };

    const entries = normalizeSchedule(response, 0, COURSES, monday);
    expect(entries[0].teachers).toEqual([]);
    expect(entries[1].teachers).toEqual([]);
  });

  it('normalises out-of-range dayOfWeekIndex into the Mon-Sun span', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 8 })],
      uniqueClasses: [],
    };
    expect(normalizeSchedule(response, 0, COURSES, monday)[0].date).toBe('2026-04-06');

    const response2: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 0 })],
      uniqueClasses: [],
    };
    expect(normalizeSchedule(response2, 0, COURSES, monday)[0].date).toBe('2026-04-12');
  });
});

describe('weekAnchorIsoDate', () => {
  const monday = new Date(2026, 3, 6);

  it('returns the Monday of the current ISO week', () => {
    expect(weekAnchorIsoDate(monday, 0)).toBe('2026-04-06');
  });

  it('shifts the anchor by weekIndex weeks', () => {
    expect(weekAnchorIsoDate(monday, 1)).toBe('2026-04-13');
    expect(weekAnchorIsoDate(monday, -1)).toBe('2026-03-30');
  });

  it('aligns to Monday when invoked on a Sunday', () => {
    const sunday = new Date(2026, 3, 12);
    expect(weekAnchorIsoDate(sunday, 0)).toBe('2026-04-06');
  });

  it('matches the Monday occurrence date that normalizeSchedule assigns', () => {
    const response: GetCourseScheduleDTO = {
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 1 })],
      uniqueClasses: [],
    };
    for (const weekIndex of [-2, -1, 0, 1, 3]) {
      expect(weekAnchorIsoDate(monday, weekIndex)).toBe(
        normalizeSchedule(response, weekIndex, COURSES, monday)[0].date,
      );
    }
  });
});
