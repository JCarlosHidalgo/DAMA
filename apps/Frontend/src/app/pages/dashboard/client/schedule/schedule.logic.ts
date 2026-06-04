import { HttpErrorResponse } from '@angular/common/http';

import { ClassGroup, CourseScheduleEntry, UserListItem } from '@core/models';

export type FormKind = 'scheduled' | 'unique';

export interface ClassDialogResult {
  kind: FormKind;
  courseId: string;
  groupId: string;
  teacherIds: string[];
  dayOfWeekIndex: number;
  date: string;
  startTime: string;
  endTime: string;
  maxStudentLimit: number;
}

export const DAY_OF_WEEK_OPTIONS = [
  { value: 1, label: 'Lunes' },
  { value: 2, label: 'Martes' },
  { value: 3, label: 'Miércoles' },
  { value: 4, label: 'Jueves' },
  { value: 5, label: 'Viernes' },
  { value: 6, label: 'Sábado' },
  { value: 7, label: 'Domingo' },
] as const;

export function isValidClassForm(value: {
  kind: FormKind;
  startTime: string;
  endTime: string;
  date: string;
}): boolean {
  if (value.startTime >= value.endTime) {
    return false;
  }
  if (value.kind === 'unique' && !value.date) {
    return false;
  }
  return true;
}

export function kindLabel(entry: CourseScheduleEntry): string {
  return entry.classKind === 'Scheduled' ? 'Semanal' : 'Única';
}

export function teacherNames(entry: CourseScheduleEntry): string {
  return entry.teachers.map((teacher) => teacher.teacherName).join(', ') || 'Sin profesor';
}

export function weekdayIndexOf(entry: CourseScheduleEntry): number {
  if (entry.dayOfWeekIndex) {
    return entry.dayOfWeekIndex;
  }
  const [year, month, day] = entry.date.split('-').map(Number);
  const weekday = new Date(year, month - 1, day).getDay();
  return weekday === 0 ? 7 : weekday;
}

export function sortByStartTime(entries: CourseScheduleEntry[]): CourseScheduleEntry[] {
  return [...entries].sort((first, second) => first.startTime.localeCompare(second.startTime));
}

export function findGroupById(groups: ClassGroup[], groupId: string): ClassGroup | undefined {
  return groups.find((group) => group.id === groupId);
}

export function candidateGroups(groups: ClassGroup[], selectedGroupId: string): ClassGroup[] {
  return groups.filter((group) => group.id !== selectedGroupId);
}

export function filterEntriesByGroup(
  entries: CourseScheduleEntry[],
  groupId: string,
): CourseScheduleEntry[] {
  return entries.filter((entry) => entry.groupId === groupId);
}

export function entriesForGroupAndDay(
  entries: CourseScheduleEntry[],
  groupId: string,
  dayIndex: number,
): CourseScheduleEntry[] {
  return sortByStartTime(
    entries.filter((entry) => entry.groupId === groupId && weekdayIndexOf(entry) === dayIndex),
  );
}

export function resolveSelectedGroupId(currentSelectedId: string, groups: ClassGroup[]): string {
  if (!currentSelectedId || !groups.some((group) => group.id === currentSelectedId)) {
    return groups[0]?.id ?? '';
  }
  return currentSelectedId;
}

export function resolveTargetGroupId(candidates: ClassGroup[], currentTargetId: string): string {
  if (!candidates.some((group) => group.id === currentTargetId)) {
    return candidates[0]?.id ?? '';
  }
  return currentTargetId;
}

export function nextWeekIndex(current: number, delta: number): number {
  return delta === 0 ? 0 : current + delta;
}

export interface DayDeltaOutcome {
  dayIndex: number;
  weekIndex: number;
  reload: boolean;
}

export function resolveDayDelta(
  currentDay: number,
  currentWeek: number,
  delta: number,
): DayDeltaOutcome {
  const nextDay = currentDay + delta;
  if (nextDay < 1) {
    return { dayIndex: 7, weekIndex: currentWeek - 1, reload: true };
  }
  if (nextDay > 7) {
    return { dayIndex: 1, weekIndex: currentWeek + 1, reload: true };
  }
  return { dayIndex: nextDay, weekIndex: currentWeek, reload: false };
}

export function buildTeacherPayload(
  teacherIds: string[],
  teachers: UserListItem[],
): { teacherId: string; teacherName: string }[] {
  const teacherNameById = new Map(teachers.map((teacher) => [teacher.id, teacher.username]));
  return teacherIds.map((teacherId) => ({
    teacherId,
    teacherName: teacherNameById.get(teacherId) ?? teacherId,
  }));
}

export function classifyTransferError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'La clase se solapa con otra en el grupo destino.';
  }
  return 'Error al transferir clase.';
}

export function transferConfirmMessage(courseName: string, targetGroupName: string): string {
  return `¿Mover "${courseName}" al grupo "${targetGroupName}"?`;
}

export function deleteClassMessage(courseName: string, date: string): string {
  return `¿Eliminar clase de ${courseName} el ${date}?`;
}

export function formKindForClassKind(classKind: CourseScheduleEntry['classKind']): FormKind {
  return classKind === 'Scheduled' ? 'scheduled' : 'unique';
}
