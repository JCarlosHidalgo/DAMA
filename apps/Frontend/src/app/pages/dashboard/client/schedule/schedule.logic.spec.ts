import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { ClassGroup, CourseScheduleEntry, UserListItem } from '@core/models';

import {
  buildTeacherPayload,
  candidateGroups,
  classifyTransferError,
  DAY_OF_WEEK_OPTIONS,
  deleteClassMessage,
  entriesForGroupAndDay,
  findGroupById,
  formKindForClassKind,
  isValidClassForm,
  kindLabel,
  resolveTargetGroupId,
  resolveDayDelta,
  sortByStartTime,
  teacherNames,
  transferConfirmMessage,
  weekdayIndexOf,
} from './schedule.logic';

function makeEntry(overrides: Partial<CourseScheduleEntry> = {}): CourseScheduleEntry {
  return {
    classId: 'c1',
    classKind: 'Scheduled',
    courseId: 'course-1',
    courseName: 'Yoga',
    date: '2026-06-02',
    startTime: '09:00:00',
    endTime: '10:00:00',
    teachers: [],
    dayOfWeekIndex: 1,
    maxStudentLimit: 0,
    groupId: 'g1',
    groupName: 'Group A',
    ...overrides,
  } as CourseScheduleEntry;
}

function makeGroup(id: string, name: string): ClassGroup {
  return { id, name };
}

describe('isValidClassForm', () => {
  it('returns true when startTime < endTime for scheduled kind', () => {
    expect(
      isValidClassForm({ kind: 'scheduled', startTime: '09:00', endTime: '10:00', date: '' }),
    ).toBe(true);
  });

  it('returns false when startTime equals endTime', () => {
    expect(
      isValidClassForm({ kind: 'scheduled', startTime: '09:00', endTime: '09:00', date: '' }),
    ).toBe(false);
  });

  it('returns false when startTime is after endTime', () => {
    expect(
      isValidClassForm({ kind: 'scheduled', startTime: '10:00', endTime: '09:00', date: '' }),
    ).toBe(false);
  });

  it('returns false for unique kind without a date', () => {
    expect(
      isValidClassForm({ kind: 'unique', startTime: '09:00', endTime: '10:00', date: '' }),
    ).toBe(false);
  });

  it('returns true for unique kind with a date', () => {
    expect(
      isValidClassForm({
        kind: 'unique',
        startTime: '09:00',
        endTime: '10:00',
        date: '2026-06-01',
      }),
    ).toBe(true);
  });

  it('returns true for scheduled kind without a date', () => {
    expect(
      isValidClassForm({ kind: 'scheduled', startTime: '09:00', endTime: '10:00', date: '' }),
    ).toBe(true);
  });
});

describe('kindLabel', () => {
  it('returns Semanal for Scheduled entries', () => {
    expect(kindLabel(makeEntry({ classKind: 'Scheduled' }))).toBe('Semanal');
  });

  it('returns Única for Unique entries', () => {
    expect(kindLabel(makeEntry({ classKind: 'Unique' }))).toBe('Única');
  });
});

describe('formKindForClassKind', () => {
  it('returns scheduled for Scheduled', () => {
    expect(formKindForClassKind('Scheduled')).toBe('scheduled');
  });

  it('returns unique for Unique', () => {
    expect(formKindForClassKind('Unique')).toBe('unique');
  });
});

describe('teacherNames', () => {
  it('joins teacher names with a comma', () => {
    const entry = makeEntry({
      teachers: [
        { teacherId: 't1', teacherName: 'Ana' },
        { teacherId: 't2', teacherName: 'Bob' },
      ],
    });
    expect(teacherNames(entry)).toBe('Ana, Bob');
  });

  it('returns Sin profesor when teachers array is empty', () => {
    expect(teacherNames(makeEntry({ teachers: [] }))).toBe('Sin profesor');
  });
});

describe('weekdayIndexOf', () => {
  it('returns dayOfWeekIndex when present', () => {
    const entry = makeEntry({ dayOfWeekIndex: 3 });
    expect(weekdayIndexOf(entry)).toBe(3);
  });

  it('derives weekday from date when dayOfWeekIndex is absent (weekday)', () => {
    const entry = makeEntry({ dayOfWeekIndex: undefined, date: '2026-06-03' });
    expect(weekdayIndexOf(entry)).toBe(3);
  });

  it('maps Sunday (getDay()===0) to 7', () => {
    const entry = makeEntry({ dayOfWeekIndex: undefined, date: '2026-06-07' });
    expect(weekdayIndexOf(entry)).toBe(7);
  });
});

describe('sortByStartTime', () => {
  it('sorts entries by startTime ascending', () => {
    const a = makeEntry({ classId: 'a', startTime: '10:00:00' });
    const b = makeEntry({ classId: 'b', startTime: '08:00:00' });
    const c = makeEntry({ classId: 'c', startTime: '09:00:00' });
    const sorted = sortByStartTime([a, b, c]);
    expect(sorted.map((e) => e.classId)).toEqual(['b', 'c', 'a']);
  });

  it('does not mutate the input array', () => {
    const a = makeEntry({ classId: 'a', startTime: '10:00:00' });
    const b = makeEntry({ classId: 'b', startTime: '08:00:00' });
    const input = [a, b];
    sortByStartTime(input);
    expect(input[0].classId).toBe('a');
  });
});

describe('findGroupById', () => {
  const groups = [makeGroup('g1', 'Alpha'), makeGroup('g2', 'Beta')];

  it('finds a group by id', () => {
    expect(findGroupById(groups, 'g2')?.name).toBe('Beta');
  });

  it('returns undefined when id is not found', () => {
    expect(findGroupById(groups, 'g99')).toBeUndefined();
  });
});

describe('candidateGroups', () => {
  const groups = [makeGroup('g1', 'Alpha'), makeGroup('g2', 'Beta'), makeGroup('g3', 'Gamma')];

  it('excludes the selected group', () => {
    const result = candidateGroups(groups, 'g1');
    expect(result.map((g) => g.id)).toEqual(['g2', 'g3']);
  });

  it('includes all groups when selected id is not present', () => {
    const result = candidateGroups(groups, 'g99');
    expect(result).toHaveLength(3);
  });
});

describe('entriesForGroupAndDay', () => {
  const entries = [
    makeEntry({ classId: 'a', groupId: 'g1', dayOfWeekIndex: 1, startTime: '10:00:00' }),
    makeEntry({ classId: 'b', groupId: 'g1', dayOfWeekIndex: 2, startTime: '09:00:00' }),
    makeEntry({ classId: 'c', groupId: 'g1', dayOfWeekIndex: 1, startTime: '08:00:00' }),
    makeEntry({ classId: 'd', groupId: 'g2', dayOfWeekIndex: 1, startTime: '07:00:00' }),
  ];

  it('filters by group and day and sorts by start time', () => {
    const result = entriesForGroupAndDay(entries, 'g1', 1);
    expect(result.map((e) => e.classId)).toEqual(['c', 'a']);
  });

  it('returns empty array when no entries match', () => {
    expect(entriesForGroupAndDay(entries, 'g1', 5)).toHaveLength(0);
  });
});

describe('resolveTargetGroupId', () => {
  const candidates = [makeGroup('g2', 'Beta'), makeGroup('g3', 'Gamma')];

  it('keeps current id when it exists in candidates', () => {
    expect(resolveTargetGroupId(candidates, 'g3')).toBe('g3');
  });

  it('falls back to first candidate when current id is absent', () => {
    expect(resolveTargetGroupId(candidates, 'g99')).toBe('g2');
  });

  it('returns empty string when candidates list is empty', () => {
    expect(resolveTargetGroupId([], 'g2')).toBe('');
  });
});

describe('resolveDayDelta', () => {
  it('wraps to day 7 and decrements week when next day < 1', () => {
    const result = resolveDayDelta(1, 2, -1);
    expect(result).toEqual({ dayIndex: 7, weekIndex: 1, reload: true });
  });

  it('wraps to day 1 and increments week when next day > 7', () => {
    const result = resolveDayDelta(7, 2, 1);
    expect(result).toEqual({ dayIndex: 1, weekIndex: 3, reload: true });
  });

  it('advances day within the week without reload', () => {
    const result = resolveDayDelta(3, 2, 1);
    expect(result).toEqual({ dayIndex: 4, weekIndex: 2, reload: false });
  });
});

describe('buildTeacherPayload', () => {
  const teachers: UserListItem[] = [
    { id: 't1', username: 'Ana' },
    { id: 't2', username: 'Bob' },
  ];

  it('maps known teacher ids to their names', () => {
    expect(buildTeacherPayload(['t1', 't2'], teachers)).toEqual([
      { teacherId: 't1', teacherName: 'Ana' },
      { teacherId: 't2', teacherName: 'Bob' },
    ]);
  });

  it('falls back to the id when teacher is not found', () => {
    expect(buildTeacherPayload(['t99'], teachers)).toEqual([
      { teacherId: 't99', teacherName: 't99' },
    ]);
  });
});

describe('classifyTransferError', () => {
  it('returns overlap message for 409 HttpErrorResponse', () => {
    const error = new HttpErrorResponse({ status: 409 });
    expect(classifyTransferError(error)).toBe('La clase se solapa con otra en el grupo destino.');
  });

  it('returns generic message for non-409 HttpErrorResponse', () => {
    const error = new HttpErrorResponse({ status: 500 });
    expect(classifyTransferError(error)).toBe('Error al transferir clase.');
  });

  it('returns generic message for non-HttpErrorResponse errors', () => {
    expect(classifyTransferError(new Error('oops'))).toBe('Error al transferir clase.');
  });
});

describe('transferConfirmMessage', () => {
  it('produces the confirmation message with course and group names', () => {
    expect(transferConfirmMessage('Yoga', 'Grupo B')).toBe('¿Mover "Yoga" al grupo "Grupo B"?');
  });
});

describe('deleteClassMessage', () => {
  it('produces the delete confirmation message', () => {
    expect(deleteClassMessage('Yoga', '2026-06-01')).toBe('¿Eliminar clase de Yoga el 2026-06-01?');
  });
});

describe('DAY_OF_WEEK_OPTIONS', () => {
  it('has 7 entries starting at Monday (1) through Sunday (7)', () => {
    expect(DAY_OF_WEEK_OPTIONS).toHaveLength(7);
    expect(DAY_OF_WEEK_OPTIONS[0].value).toBe(1);
    expect(DAY_OF_WEEK_OPTIONS[6].value).toBe(7);
  });
});
