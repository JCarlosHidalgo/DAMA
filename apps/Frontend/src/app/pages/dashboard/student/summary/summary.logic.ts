import type {
  ScheduledClassAttendance,
  StudentSpendPoint,
  UniqueClassAttendance,
} from '@core/models';

export interface MonthlySeries {
  labels: string[];
  values: number[];
}

function monthLabel(year: number, month: number): string {
  return `${year}-${String(month).padStart(2, '0')}`;
}

export function spendPointsToLine(points: StudentSpendPoint[]): MonthlySeries {
  return {
    labels: points.map((point) => monthLabel(point.year, point.month)),
    values: points.map((point) => point.amount),
  };
}

export function aggregateClassesPerMonth(
  scheduled: ScheduledClassAttendance[],
  unique: UniqueClassAttendance[],
): MonthlySeries {
  const countsByMonth = new Map<string, number>();

  const addEntry = (classDate: string): void => {
    const key = classDate.slice(0, 7);
    countsByMonth.set(key, (countsByMonth.get(key) ?? 0) + 1);
  };

  for (const entry of scheduled) {
    addEntry(entry.classDate);
  }
  for (const entry of unique) {
    addEntry(entry.classDate);
  }

  const sorted = [...countsByMonth.entries()].sort((left, right) =>
    left[0].localeCompare(right[0]),
  );

  return {
    labels: sorted.map((entry) => entry[0]),
    values: sorted.map((entry) => entry[1]),
  };
}
