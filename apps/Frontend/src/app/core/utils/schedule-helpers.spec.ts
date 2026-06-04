import { describe, expect, it } from 'vitest';

import { ClassGroup, Course, CourseScheduleEntry } from '@core/models';

import {
  filterEntriesByGroup,
  mergeCourses,
  missingCourseIds,
  nextWeekIndex,
  resolveSelectedGroupId,
  scheduledAttendanceKey,
  subscriptionAllowsScheduleInteraction,
} from './schedule-helpers';

function makeEntry(groupId: string): CourseScheduleEntry {
  return {
    classId: 'cls-1',
    classKind: 'Scheduled',
    courseId: 'c-1',
    courseName: 'Course',
    date: '2026-01-01',
    startTime: '08:00',
    endTime: '09:00',
    teachers: [],
    maxStudentLimit: 10,
    groupId,
    groupName: 'Group',
  } as CourseScheduleEntry;
}

function makeCourse(id: string): Course {
  return { id, name: `Course ${id}`, tenantId: 'tenant-1' };
}

function makeGroup(id: string): ClassGroup {
  return { id, name: `Group ${id}` };
}

describe('filterEntriesByGroup', () => {
  it('returns all entries when groupId is empty', () => {
    const entries = [makeEntry('g1'), makeEntry('g2')];
    expect(filterEntriesByGroup(entries, '')).toEqual(entries);
  });

  it('returns only entries matching the given groupId', () => {
    const matching = makeEntry('g1');
    const other = makeEntry('g2');
    const result = filterEntriesByGroup([matching, other], 'g1');
    expect(result).toEqual([matching]);
  });
});

describe('resolveSelectedGroupId', () => {
  it('returns the first group id when current is empty', () => {
    const groups = [makeGroup('g1'), makeGroup('g2')];
    expect(resolveSelectedGroupId('', groups)).toBe('g1');
  });

  it("returns '' when current is empty and groups array is empty", () => {
    expect(resolveSelectedGroupId('', [])).toBe('');
  });

  it('returns the first group id when current is not in groups', () => {
    const groups = [makeGroup('g1'), makeGroup('g2')];
    expect(resolveSelectedGroupId('g99', groups)).toBe('g1');
  });

  it('keeps current when it is present in groups', () => {
    const groups = [makeGroup('g1'), makeGroup('g2')];
    expect(resolveSelectedGroupId('g2', groups)).toBe('g2');
  });
});

describe('nextWeekIndex', () => {
  it('returns 0 when delta is 0', () => {
    expect(nextWeekIndex(3, 0)).toBe(0);
  });

  it('increments current by positive delta', () => {
    expect(nextWeekIndex(3, 1)).toBe(4);
  });

  it('decrements current by negative delta', () => {
    expect(nextWeekIndex(3, -1)).toBe(2);
  });
});

describe('missingCourseIds', () => {
  it('returns empty array when both arrays are undefined', () => {
    expect(missingCourseIds({}, [makeCourse('c1')])).toEqual([]);
  });

  it('returns empty array when all course ids are known', () => {
    const known = [makeCourse('c1'), makeCourse('c2')];
    const response = {
      scheduledClasses: [{ courseId: 'c1' }],
      uniqueClasses: [{ courseId: 'c2' }],
    };
    expect(missingCourseIds(response, known)).toEqual([]);
  });

  it('collects missing ids from scheduledClasses', () => {
    const response = { scheduledClasses: [{ courseId: 'c1' }], uniqueClasses: [] };
    expect(missingCourseIds(response, [])).toEqual(['c1']);
  });

  it('collects missing ids from uniqueClasses', () => {
    const response = { scheduledClasses: [], uniqueClasses: [{ courseId: 'c2' }] };
    expect(missingCourseIds(response, [])).toEqual(['c2']);
  });

  it('deduplicates ids that appear in both lists', () => {
    const response = {
      scheduledClasses: [{ courseId: 'c1' }],
      uniqueClasses: [{ courseId: 'c1' }],
    };
    expect(missingCourseIds(response, [])).toEqual(['c1']);
  });

  it('excludes ids already present in knownCourses', () => {
    const response = {
      scheduledClasses: [{ courseId: 'c1' }, { courseId: 'c2' }],
      uniqueClasses: [],
    };
    expect(missingCourseIds(response, [makeCourse('c1')])).toEqual(['c2']);
  });
});

describe('mergeCourses', () => {
  it('appends non-null fetched courses to existing', () => {
    const existing = [makeCourse('c1')];
    const fetched = [makeCourse('c2'), makeCourse('c3')];
    const result = mergeCourses(existing, fetched);
    expect(result.map((course) => course.id)).toEqual(['c1', 'c2', 'c3']);
  });

  it('skips null entries in fetched', () => {
    const existing = [makeCourse('c1')];
    const result = mergeCourses(existing, [null, makeCourse('c2')]);
    expect(result.map((course) => course.id)).toEqual(['c1', 'c2']);
  });

  it('preserves existing when fetched is empty', () => {
    const existing = [makeCourse('c1')];
    const result = mergeCourses(existing, []);
    expect(result).toEqual(existing);
  });
});

describe('subscriptionAllowsScheduleInteraction', () => {
  it('returns false for index below 2', () => {
    expect(subscriptionAllowsScheduleInteraction(1)).toBe(false);
  });

  it('returns true for index 2', () => {
    expect(subscriptionAllowsScheduleInteraction(2)).toBe(true);
  });
});

describe('scheduledAttendanceKey', () => {
  it('formats key as classId pipe classDate', () => {
    expect(scheduledAttendanceKey('c', '2026-06-03')).toBe('c|2026-06-03');
  });
});
