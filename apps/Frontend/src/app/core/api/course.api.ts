import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '@env/environment';
import {
  Course,
  CreateCoursePayload,
  UpdateCoursePayload,
  ScheduledClass,
  UniqueClass,
  CreateScheduledClassPayload,
  UpdateScheduledClassPayload,
  CreateUniqueClassPayload,
  UpdateUniqueClassPayload,
  GetCourseScheduleDTO,
  ClassGroup,
} from '@core/models';

@Injectable({ providedIn: 'root' })
export class CourseApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/course-management/course`;

  listCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(this.base);
  }

  getCourse(id: string): Observable<Course> {
    return this.http.get<Course>(`${this.base}/${id}`);
  }

  createCourse(payload: CreateCoursePayload): Observable<Course> {
    return this.http.post<Course>(this.base, payload);
  }

  updateCourse(id: string, payload: UpdateCoursePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, payload);
  }

  deleteCourse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  createScheduledClass(payload: CreateScheduledClassPayload): Observable<ScheduledClass> {
    return this.http.post<ScheduledClass>(`${this.base}/scheduled`, payload);
  }

  updateScheduledClass(id: string, payload: UpdateScheduledClassPayload): Observable<void> {
    return this.http.put<void>(`${this.base}/scheduled/${id}`, payload);
  }

  deleteScheduledClass(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/scheduled/${id}`);
  }

  createUniqueClass(payload: CreateUniqueClassPayload): Observable<UniqueClass> {
    return this.http.post<UniqueClass>(`${this.base}/unique`, payload);
  }

  updateUniqueClass(id: string, payload: UpdateUniqueClassPayload): Observable<void> {
    return this.http.put<void>(`${this.base}/unique/${id}`, payload);
  }

  deleteUniqueClass(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/unique/${id}`);
  }

  getSchedule(courseId: string, weekPaginationIndex: number): Observable<GetCourseScheduleDTO> {
    return this.http.get<GetCourseScheduleDTO>(`${this.base}/schedule`, {
      params: new HttpParams()
        .set('CourseId', courseId)
        .set('WeekPaginationIndex', weekPaginationIndex),
    });
  }

  getTeacherSchedule(weekPaginationIndex: number): Observable<GetCourseScheduleDTO> {
    return this.http.get<GetCourseScheduleDTO>(`${this.base}/teacher/me`, {
      params: new HttpParams().set('WeekPaginationIndex', weekPaginationIndex),
    });
  }

  getTenantSchedule(weekPaginationIndex: number): Observable<GetCourseScheduleDTO> {
    return this.http.get<GetCourseScheduleDTO>(`${this.base}/tenant/schedule`, {
      params: new HttpParams().set('WeekPaginationIndex', weekPaginationIndex),
    });
  }

  getStudentSchedule(weekPaginationIndex: number): Observable<GetCourseScheduleDTO> {
    return this.http.get<GetCourseScheduleDTO>(`${this.base}/student/schedule`, {
      params: new HttpParams().set('WeekPaginationIndex', weekPaginationIndex),
    });
  }

  getGroups(): Observable<ClassGroup[]> {
    return this.http.get<ClassGroup[]>(`${this.base}/group`);
  }

  getTeacherGroups(): Observable<ClassGroup[]> {
    return this.http.get<ClassGroup[]>(`${this.base}/group/teacher/me`);
  }

  createGroup(name: string): Observable<ClassGroup> {
    return this.http.post<ClassGroup>(`${this.base}/group`, { name });
  }

  renameGroup(id: string, name: string): Observable<void> {
    return this.http.put<void>(`${this.base}/group/${id}`, { name });
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/group/${id}`);
  }

  transferScheduledClass(id: string, targetGroupId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/scheduled/${id}/transfer`, { targetGroupId });
  }

  transferUniqueClass(id: string, targetGroupId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/unique/${id}/transfer`, { targetGroupId });
  }
}
