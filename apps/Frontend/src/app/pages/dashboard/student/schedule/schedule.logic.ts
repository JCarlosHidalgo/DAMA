import { CourseScheduleEntry } from '@core/models';
import { scheduledAttendanceKey } from '@core/utils';

export function studentScheduleSubtitle(interactable: boolean): string {
  return interactable ? 'Toca una clase para confirmar tu asistencia' : 'Vista de solo lectura';
}

export function isEntryAlreadyMarked(
  entry: CourseScheduleEntry,
  scheduledKeys: Set<string>,
  uniqueIds: Set<string>,
): boolean {
  if (entry.classKind === 'Scheduled') {
    return scheduledKeys.has(scheduledAttendanceKey(entry.classId, entry.date));
  }
  return uniqueIds.has(entry.classId);
}
