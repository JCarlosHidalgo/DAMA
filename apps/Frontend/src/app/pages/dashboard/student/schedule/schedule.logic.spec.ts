import { describe, expect, it } from 'vitest';

import { CourseScheduleEntry } from '@core/models';

import { isEntryAlreadyMarked, studentScheduleSubtitle } from './schedule.logic';

function makeEntry(
  groupId: string,
  classKind: 'Scheduled' | 'Unique' = 'Scheduled',
): CourseScheduleEntry {
  return {
    classId: 'cls-1',
    classKind,
    courseId: 'c-1',
    courseName: 'Course',
    date: '2026-06-03',
    startTime: '08:00',
    endTime: '09:00',
    teachers: [],
    maxStudentLimit: 10,
    groupId,
    groupName: 'Group',
  };
}

describe('studentScheduleSubtitle', () => {
  it('returns the interactable subtitle when true', () => {
    expect(studentScheduleSubtitle(true)).toBe('Toca una clase para confirmar tu asistencia');
  });

  it('returns the read-only subtitle when false', () => {
    expect(studentScheduleSubtitle(false)).toBe('Vista de solo lectura');
  });
});

describe('isEntryAlreadyMarked', () => {
  it('returns true for Scheduled entry whose key is in scheduledKeys', () => {
    const entry = makeEntry('g1', 'Scheduled');
    const scheduledKeys = new Set(['cls-1|2026-06-03']);
    expect(isEntryAlreadyMarked(entry, scheduledKeys, new Set())).toBe(true);
  });

  it('returns false for Scheduled entry whose key is absent from scheduledKeys', () => {
    const entry = makeEntry('g1', 'Scheduled');
    expect(isEntryAlreadyMarked(entry, new Set(), new Set())).toBe(false);
  });

  it('returns true for Unique entry whose classId is in uniqueIds', () => {
    const entry = makeEntry('g1', 'Unique');
    const uniqueIds = new Set(['cls-1']);
    expect(isEntryAlreadyMarked(entry, new Set(), uniqueIds)).toBe(true);
  });

  it('returns false for Unique entry whose classId is absent from uniqueIds', () => {
    const entry = makeEntry('g1', 'Unique');
    expect(isEntryAlreadyMarked(entry, new Set(), new Set())).toBe(false);
  });
});
