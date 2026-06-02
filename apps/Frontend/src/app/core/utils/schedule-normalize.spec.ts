import { describe, it, expect } from 'vitest';

import { isoWeekdayIndex, normalizeSchedule } from './schedule-normalize';
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

function buildResponse(overrides: Partial<GetCourseScheduleDTO> = {}): GetCourseScheduleDTO {
  return {
    scheduledClasses: [],
    uniqueClasses: [],
    weekStartDate: '2026-04-06',
    todayDate: '2026-04-08',
    ...overrides,
  };
}

describe('normalizeSchedule', () => {
  it('returns an empty list when there are no classes', () => {
    expect(normalizeSchedule(buildResponse(), COURSES)).toEqual([]);
  });

  it('handles missing arrays as if they were empty', () => {
    const response = { weekStartDate: '2026-04-06', todayDate: '2026-04-08' } as GetCourseScheduleDTO;

    expect(normalizeSchedule(response, COURSES)).toEqual([]);
  });

  it('maps scheduled classes off the backend week-start date', () => {
    const response = buildResponse({
      scheduledClasses: [
        buildScheduledClass({ dayOfWeekIndex: 1 }),
        buildScheduledClass({ id: 'sched-2', dayOfWeekIndex: 5 }),
      ],
    });

    const entries = normalizeSchedule(response, COURSES);

    expect(entries).toHaveLength(2);
    expect(entries[0].date).toBe('2026-04-06');
    expect(entries[1].date).toBe('2026-04-10');
    expect(entries[0].classKind).toBe('Scheduled');
    expect(entries[0].courseName).toBe('Yoga');
  });

  it('anchors on whatever week-start the backend returns', () => {
    const response = buildResponse({
      weekStartDate: '2026-04-13',
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 1 })],
    });

    expect(normalizeSchedule(response, COURSES)[0].date).toBe('2026-04-13');
  });

  it('maps unique classes preserving their date and merging with scheduled', () => {
    const response = buildResponse({
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 2 })],
      uniqueClasses: [buildUniqueClass({ date: '2026-04-10' })],
    });

    const entries = normalizeSchedule(response, COURSES);

    expect(entries).toHaveLength(2);
    expect(entries[1].classKind).toBe('Unique');
    expect(entries[1].date).toBe('2026-04-10');
    expect(entries[1].courseName).toBe('Pilates');
    expect(entries[1].dayOfWeekIndex).toBeUndefined();
  });

  it('falls back to "—" when the course is unknown', () => {
    const response = buildResponse({
      scheduledClasses: [buildScheduledClass({ courseId: 'missing' })],
      uniqueClasses: [buildUniqueClass({ courseId: 'also-missing' })],
    });

    const entries = normalizeSchedule(response, COURSES);
    expect(entries[0].courseName).toBe('—');
    expect(entries[1].courseName).toBe('—');
  });

  it('uses empty teachers array when teachers are missing from a class', () => {
    const response = buildResponse({
      scheduledClasses: [buildScheduledClass({ teachers: undefined as unknown as never })],
      uniqueClasses: [buildUniqueClass({ teachers: undefined as unknown as never })],
    });

    const entries = normalizeSchedule(response, COURSES);
    expect(entries[0].teachers).toEqual([]);
    expect(entries[1].teachers).toEqual([]);
  });

  it('normalises out-of-range dayOfWeekIndex into the Mon-Sun span', () => {
    const eighthDay = buildResponse({
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 8 })],
    });
    expect(normalizeSchedule(eighthDay, COURSES)[0].date).toBe('2026-04-06');

    const zeroDay = buildResponse({
      scheduledClasses: [buildScheduledClass({ dayOfWeekIndex: 0 })],
    });
    expect(normalizeSchedule(zeroDay, COURSES)[0].date).toBe('2026-04-12');
  });
});

describe('isoWeekdayIndex', () => {
  it('maps Monday to 1', () => {
    expect(isoWeekdayIndex('2026-04-06')).toBe(1);
  });

  it('maps Friday to 5', () => {
    expect(isoWeekdayIndex('2026-04-10')).toBe(5);
  });

  it('maps Sunday to 7', () => {
    expect(isoWeekdayIndex('2026-04-12')).toBe(7);
  });
});
