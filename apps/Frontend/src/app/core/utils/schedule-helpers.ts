import { ClassGroup, Course, CourseScheduleEntry } from '@core/models';

export function filterEntriesByGroup(
  entries: CourseScheduleEntry[],
  groupId: string,
): CourseScheduleEntry[] {
  if (!groupId) {
    return entries;
  }
  return entries.filter((entry) => entry.groupId === groupId);
}

export function resolveSelectedGroupId(currentSelectedId: string, groups: ClassGroup[]): string {
  if (!currentSelectedId || !groups.some((group) => group.id === currentSelectedId)) {
    return groups[0]?.id ?? '';
  }
  return currentSelectedId;
}

export function nextWeekIndex(current: number, delta: number): number {
  return delta === 0 ? 0 : current + delta;
}

export function missingCourseIds(
  scheduleResponse: {
    scheduledClasses?: { courseId: string }[];
    uniqueClasses?: { courseId: string }[];
  },
  knownCourses: Course[],
): string[] {
  const knownIds = new Set(knownCourses.map((course) => course.id));
  const missing = new Set<string>();
  for (const scheduledClass of scheduleResponse.scheduledClasses ?? []) {
    if (!knownIds.has(scheduledClass.courseId)) {
      missing.add(scheduledClass.courseId);
    }
  }
  for (const uniqueClass of scheduleResponse.uniqueClasses ?? []) {
    if (!knownIds.has(uniqueClass.courseId)) {
      missing.add(uniqueClass.courseId);
    }
  }
  return Array.from(missing);
}

export function mergeCourses(existing: Course[], fetched: (Course | null)[]): Course[] {
  const merged = [...existing];
  for (const course of fetched) {
    if (course) {
      merged.push(course);
    }
  }
  return merged;
}

export function subscriptionAllowsScheduleInteraction(subscriptionIndex: number): boolean {
  return subscriptionIndex >= 2;
}

export function scheduledAttendanceKey(classId: string, classDate: string): string {
  return `${classId}|${classDate}`;
}
