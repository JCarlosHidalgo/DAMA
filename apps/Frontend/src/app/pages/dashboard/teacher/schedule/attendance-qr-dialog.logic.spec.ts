import { describe, it, expect } from 'vitest';

import { CourseScheduleEntry } from '@core/models';
import { ClassKind, RosterEntry } from '@core/strategies';

import {
  attendanceEntrySubtitle,
  newRosterAdditions,
  qrKindFromClassKind,
} from './attendance-qr-dialog.logic';

describe('qrKindFromClassKind', () => {
  it("maps 'Scheduled' to 'SCHEDULED'", () => {
    const classKind: ClassKind = 'Scheduled';
    expect(qrKindFromClassKind(classKind)).toBe('SCHEDULED');
  });

  it("maps 'Unique' to 'UNIQUE'", () => {
    const classKind: ClassKind = 'Unique';
    expect(qrKindFromClassKind(classKind)).toBe('UNIQUE');
  });
});

describe('newRosterAdditions', () => {
  const makeEntry = (studentId: string): RosterEntry =>
    ({
      studentId,
      studentName: `Student ${studentId}`,
      tenantId: 't1',
      classId: 'c1',
      classDate: '2026-01-01',
      startTime: '09:00',
      endTime: '10:00',
      courseName: 'Yoga',
    }) as RosterEntry;

  it('returns only genuinely new entries when some are already present', () => {
    const current = [makeEntry('s1'), makeEntry('s2')];
    const incoming = [makeEntry('s2'), makeEntry('s3'), makeEntry('s4')];
    const result = newRosterAdditions(current, incoming);
    expect(result.map((entry) => entry.studentId)).toEqual(['s3', 's4']);
  });

  it('returns empty array when all incoming are already present', () => {
    const current = [makeEntry('s1'), makeEntry('s2')];
    const incoming = [makeEntry('s1'), makeEntry('s2')];
    expect(newRosterAdditions(current, incoming)).toEqual([]);
  });

  it('returns empty array when incoming is empty', () => {
    const current = [makeEntry('s1')];
    expect(newRosterAdditions(current, [])).toEqual([]);
  });

  it('returns all incoming when current is empty', () => {
    const incoming = [makeEntry('s1'), makeEntry('s2')];
    const result = newRosterAdditions([], incoming);
    expect(result.map((entry) => entry.studentId)).toEqual(['s1', 's2']);
  });
});

describe('attendanceEntrySubtitle', () => {
  it('formats date · startTime – endTime', () => {
    const entry = {
      date: '2026-06-03',
      startTime: '09:00',
      endTime: '10:30',
    } as unknown as CourseScheduleEntry;
    expect(attendanceEntrySubtitle(entry)).toBe('2026-06-03 · 09:00 – 10:30');
  });
});
