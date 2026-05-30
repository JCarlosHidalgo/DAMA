import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AttendanceApi } from '../api/attendance.api';
import { AttendanceRealtimeService } from '../api/attendance-realtime-service';
import { CourseApi } from '../api/course.api';
import { ClassTeacherPayload, ScheduledClass, UniqueClass } from '../models/course.model';
import { ScheduledClassAttendance, UniqueClassAttendance } from '../models/attendance.model';

export type ClassKind = 'Scheduled' | 'Unique';

export interface ClassFormPayload {
  courseId: string;
  teachers: ClassTeacherPayload[];
  startTime: string;
  endTime: string;
  dayOfWeekIndex: number;
  date: string;
  maxStudentLimit: number;
}

export type RosterEntry = ScheduledClassAttendance | UniqueClassAttendance;

export interface AttendanceTarget {
  classId: string;
  courseName: string;
  classDate: string;
}

export interface ClassKindStrategy {
  readonly kind: ClassKind;
  create(payload: ClassFormPayload): Observable<ScheduledClass | UniqueClass>;
  update(classId: string, payload: ClassFormPayload): Observable<void>;
  delete(classId: string): Observable<void>;
  markAttendance(payload: { classId: string; courseName: string }): Observable<void>;
  fetchRoster(target: AttendanceTarget): Observable<RosterEntry[]>;
  connectRealtime(target: AttendanceTarget): Observable<RosterEntry>;
}

@Injectable({ providedIn: 'root' })
export class ScheduledClassStrategy implements ClassKindStrategy {
  readonly kind: ClassKind = 'Scheduled';
  private readonly courseApi = inject(CourseApi);
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly realtime = inject(AttendanceRealtimeService);

  create(payload: ClassFormPayload): Observable<ScheduledClass> {
    return this.courseApi.createScheduledClass({
      courseId: payload.courseId,
      dayOfWeekIndex: payload.dayOfWeekIndex,
      maxStudentLimit: payload.maxStudentLimit,
      startTime: payload.startTime,
      endTime: payload.endTime,
      teachers: payload.teachers,
    });
  }

  update(classId: string, payload: ClassFormPayload): Observable<void> {
    return this.courseApi.updateScheduledClass(classId, {
      dayOfWeekIndex: payload.dayOfWeekIndex,
      maxStudentLimit: payload.maxStudentLimit,
      startTime: payload.startTime,
      endTime: payload.endTime,
      teachers: payload.teachers,
    });
  }

  delete(classId: string): Observable<void> {
    return this.courseApi.deleteScheduledClass(classId);
  }

  markAttendance(payload: { classId: string; courseName: string }): Observable<void> {
    return this.attendanceApi.markScheduled(payload);
  }

  fetchRoster(target: AttendanceTarget): Observable<RosterEntry[]> {
    return this.attendanceApi.scheduledRoster(target.classId, target.classDate);
  }

  connectRealtime(target: AttendanceTarget): Observable<RosterEntry> {
    return this.realtime.connectToScheduled(target.classId, target.classDate);
  }
}

@Injectable({ providedIn: 'root' })
export class UniqueClassStrategy implements ClassKindStrategy {
  readonly kind: ClassKind = 'Unique';
  private readonly courseApi = inject(CourseApi);
  private readonly attendanceApi = inject(AttendanceApi);
  private readonly realtime = inject(AttendanceRealtimeService);

  create(payload: ClassFormPayload): Observable<UniqueClass> {
    return this.courseApi.createUniqueClass({
      courseId: payload.courseId,
      date: payload.date,
      maxStudentLimit: payload.maxStudentLimit,
      startTime: payload.startTime,
      endTime: payload.endTime,
      teachers: payload.teachers,
    });
  }

  update(classId: string, payload: ClassFormPayload): Observable<void> {
    return this.courseApi.updateUniqueClass(classId, {
      date: payload.date,
      maxStudentLimit: payload.maxStudentLimit,
      startTime: payload.startTime,
      endTime: payload.endTime,
      teachers: payload.teachers,
    });
  }

  delete(classId: string): Observable<void> {
    return this.courseApi.deleteUniqueClass(classId);
  }

  markAttendance(payload: { classId: string; courseName: string }): Observable<void> {
    return this.attendanceApi.markUnique(payload);
  }

  fetchRoster(target: AttendanceTarget): Observable<RosterEntry[]> {
    return this.attendanceApi.uniqueRoster(target.classId);
  }

  connectRealtime(target: AttendanceTarget): Observable<RosterEntry> {
    return this.realtime.connectToUnique(target.classId);
  }
}

@Injectable({ providedIn: 'root' })
export class ClassKindStrategies {
  private readonly scheduled = inject(ScheduledClassStrategy);
  private readonly unique = inject(UniqueClassStrategy);

  for(kind: ClassKind): ClassKindStrategy {
    return kind === 'Scheduled' ? this.scheduled : this.unique;
  }
}
