import { describe, expect, it } from 'vitest';

import { ScheduledClassAttendance, StudentSpendPoint, UniqueClassAttendance } from '@core/models';

import { aggregateClassesPerMonth, spendPointsToLine } from './summary.logic';

function scheduled(classDate: string): ScheduledClassAttendance {
  return {
    tenantId: 't',
    classId: 'c',
    classDate,
    startTime: '08:00',
    endTime: '09:00',
    courseName: 'Course',
    studentId: 's',
    studentName: 'Student',
  };
}

function unique(classDate: string): UniqueClassAttendance {
  return { ...scheduled(classDate) };
}

describe('spendPointsToLine', () => {
  it('maps points to padded YYYY-MM labels and amounts', () => {
    const points: StudentSpendPoint[] = [
      { year: 2026, month: 1, amount: 150, count: 2 },
      { year: 2026, month: 11, amount: 90, count: 1 },
    ];

    expect(spendPointsToLine(points)).toEqual({
      labels: ['2026-01', '2026-11'],
      values: [150, 90],
    });
  });
});

describe('aggregateClassesPerMonth', () => {
  it('counts scheduled and unique attendance grouped by month, sorted ascending', () => {
    const result = aggregateClassesPerMonth(
      [scheduled('2026-01-05'), scheduled('2026-01-20'), scheduled('2026-03-02')],
      [unique('2026-01-15'), unique('2026-02-01')],
    );

    expect(result).toEqual({
      labels: ['2026-01', '2026-02', '2026-03'],
      values: [3, 1, 1],
    });
  });

  it('returns empty series when there is no attendance', () => {
    expect(aggregateClassesPerMonth([], [])).toEqual({ labels: [], values: [] });
  });
});
