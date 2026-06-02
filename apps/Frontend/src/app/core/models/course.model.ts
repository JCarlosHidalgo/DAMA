export interface Course {
  id: string;
  name: string;
  tenantId: string;
}

export interface CreateCoursePayload {
  name: string;
  externalReference?: string | null;
}

export interface UpdateCoursePayload {
  name: string;
}

export interface ClassTeacher {
  teacherId: string;
  teacherName: string;
}

export interface ClassGroup {
  id: string;
  name: string;
}

export interface ScheduledClass {
  id: string;
  courseId: string;
  courseName: string;
  tenantId: string;
  dayOfWeekIndex: number;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  teachers: ClassTeacher[];
  groupId: string;
  groupName: string;
}

export interface UniqueClass {
  id: string;
  courseId: string;
  courseName: string;
  tenantId: string;
  date: string;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  teachers: ClassTeacher[];
  groupId: string;
  groupName: string;
}

export interface ClassTeacherPayload {
  teacherId: string;
  teacherName: string;
}

export interface CreateScheduledClassPayload {
  courseId: string;
  groupId: string;
  dayOfWeekIndex: number;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  teachers: ClassTeacherPayload[];
  externalReference?: string | null;
}

export interface UpdateScheduledClassPayload {
  dayOfWeekIndex: number;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  teachers: ClassTeacherPayload[];
}

export interface CreateUniqueClassPayload {
  courseId: string;
  groupId: string;
  date: string;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  teachers: ClassTeacherPayload[];
  externalReference?: string | null;
}

export interface UpdateUniqueClassPayload {
  date: string;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  teachers: ClassTeacherPayload[];
}

export interface CourseScheduleParameters {
  courseId: string;
  classDatePointer: string;
}

export interface GetScheduledClassDTO {
  id: string;
  dayOfWeekIndex: number;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  courseId: string;
  teachers: ClassTeacher[];
  groupId: string;
  groupName: string;
}

export interface GetUniqueClassDTO {
  id: string;
  date: string;
  maxStudentLimit: number;
  startTime: string;
  endTime: string;
  courseId: string;
  teachers: ClassTeacher[];
  groupId: string;
  groupName: string;
}

export interface GetCourseScheduleDTO {
  scheduledClasses: GetScheduledClassDTO[];
  uniqueClasses: GetUniqueClassDTO[];
  weekStartDate: string;
  todayDate: string;
}

export interface CourseScheduleEntry {
  classId: string;
  classKind: 'Scheduled' | 'Unique';
  courseId: string;
  courseName: string;
  date: string;
  startTime: string;
  endTime: string;
  teachers: ClassTeacher[];
  dayOfWeekIndex?: number;
  maxStudentLimit: number;
  groupId: string;
  groupName: string;
}
