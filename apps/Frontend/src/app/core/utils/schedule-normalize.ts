import { Course, CourseScheduleEntry, GetCourseScheduleDTO } from '@core/models';

function startOfIsoWeek(date: Date): Date {
  const copy = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const dayOfWeek = copy.getDay();
  const offsetToMonday = (dayOfWeek + 6) % 7;
  copy.setDate(copy.getDate() - offsetToMonday);
  return copy;
}

function formatIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function normalizeSchedule(
  scheduleResponse: GetCourseScheduleDTO,
  weekIndex: number,
  courses: Course[],
  todayLocal: Date,
): CourseScheduleEntry[] {
  const courseNameById = new Map(courses.map((course) => [course.id, course.name]));
  const weekStart = startOfIsoWeek(todayLocal);
  weekStart.setDate(weekStart.getDate() + weekIndex * 7);

  const entries: CourseScheduleEntry[] = [];

  for (const scheduledClass of scheduleResponse.scheduledClasses ?? []) {
    const offsetFromMonday = (((scheduledClass.dayOfWeekIndex - 1) % 7) + 7) % 7;
    const occurrenceDate = new Date(weekStart);
    occurrenceDate.setDate(occurrenceDate.getDate() + offsetFromMonday);
    entries.push({
      classId: scheduledClass.id,
      classKind: 'Scheduled',
      courseId: scheduledClass.courseId,
      courseName: courseNameById.get(scheduledClass.courseId) ?? '—',
      date: formatIsoDate(occurrenceDate),
      startTime: scheduledClass.startTime,
      endTime: scheduledClass.endTime,
      teachers: scheduledClass.teachers ?? [],
      dayOfWeekIndex: scheduledClass.dayOfWeekIndex,
      maxStudentLimit: scheduledClass.maxStudentLimit,
      groupId: scheduledClass.groupId,
      groupName: scheduledClass.groupName,
    });
  }

  for (const uniqueClass of scheduleResponse.uniqueClasses ?? []) {
    entries.push({
      classId: uniqueClass.id,
      classKind: 'Unique',
      courseId: uniqueClass.courseId,
      courseName: courseNameById.get(uniqueClass.courseId) ?? '—',
      date: uniqueClass.date,
      startTime: uniqueClass.startTime,
      endTime: uniqueClass.endTime,
      teachers: uniqueClass.teachers ?? [],
      maxStudentLimit: uniqueClass.maxStudentLimit,
      groupId: uniqueClass.groupId,
      groupName: uniqueClass.groupName,
    });
  }

  return entries;
}
