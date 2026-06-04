import { CourseScheduleEntry } from '@core/models';
import { ClassKind, RosterEntry } from '@core/strategies';

export function qrKindFromClassKind(classKind: ClassKind): 'SCHEDULED' | 'UNIQUE' {
  return classKind === 'Scheduled' ? 'SCHEDULED' : 'UNIQUE';
}

export function newRosterAdditions(current: RosterEntry[], incoming: RosterEntry[]): RosterEntry[] {
  const knownIds = new Set(current.map((row) => row.studentId));
  return incoming.filter((row) => !knownIds.has(row.studentId));
}

export function attendanceEntrySubtitle(entry: CourseScheduleEntry): string {
  return `${entry.date} · ${entry.startTime} – ${entry.endTime}`;
}
