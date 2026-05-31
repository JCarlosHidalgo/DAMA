import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '@env/environment';
import {
  StudentRemainClasses,
  ScheduledClassAttendance,
  UniqueClassAttendance,
  ScheduledAttendancePayload,
  UniqueAttendancePayload,
  ClientIncrementRemainPayload,
  Page,
} from '@core/models';

@Injectable({ providedIn: 'root' })
export class AttendanceApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/attendance`;

  getMyRemain(): Observable<StudentRemainClasses> {
    return this.http.get<StudentRemainClasses>(`${this.base}/remain/me`);
  }

  getStudentRemain(studentId: string): Observable<StudentRemainClasses> {
    return this.http.get<StudentRemainClasses>(`${this.base}/remain/${studentId}`);
  }

  clientIncrementStudent(
    studentId: string,
    payload: ClientIncrementRemainPayload,
  ): Observable<void> {
    return this.http.post<void>(`${this.base}/remain/client/${studentId}`, payload);
  }

  clientIncrementTenant(payload: ClientIncrementRemainPayload): Observable<{ affected: number }> {
    return this.http.post<{ affected: number }>(`${this.base}/remain/client/tenant`, payload);
  }

  markScheduled(payload: ScheduledAttendancePayload): Observable<void> {
    return this.http.post<void>(`${this.base}/attendance/scheduled`, payload);
  }

  markUnique(payload: UniqueAttendancePayload): Observable<void> {
    return this.http.post<void>(`${this.base}/attendance/unique`, payload);
  }

  scheduledRoster(classId: string, classDate: string): Observable<ScheduledClassAttendance[]> {
    return this.http.get<ScheduledClassAttendance[]>(
      `${this.base}/attendance/scheduled/class/${classId}/${classDate}`,
    );
  }

  uniqueRoster(classId: string): Observable<UniqueClassAttendance[]> {
    return this.http.get<UniqueClassAttendance[]>(
      `${this.base}/attendance/unique/class/${classId}`,
    );
  }

  myScheduledHistory(studentId: string): Observable<ScheduledClassAttendance[]> {
    return this.http.get<ScheduledClassAttendance[]>(
      `${this.base}/attendance/scheduled/student/${studentId}`,
    );
  }

  myUniqueHistory(studentId: string): Observable<UniqueClassAttendance[]> {
    return this.http.get<UniqueClassAttendance[]>(
      `${this.base}/attendance/unique/student/${studentId}`,
    );
  }

  listMyScheduledAttendance(pageIndex = 0): Observable<Page<ScheduledClassAttendance>> {
    return this.http.get<Page<ScheduledClassAttendance>>(`${this.base}/attendance/scheduled/me`, {
      params: new HttpParams().set('Index', pageIndex),
    });
  }

  listMyUniqueAttendance(pageIndex = 0): Observable<Page<UniqueClassAttendance>> {
    return this.http.get<Page<UniqueClassAttendance>>(`${this.base}/attendance/unique/me`, {
      params: new HttpParams().set('Index', pageIndex),
    });
  }
}
