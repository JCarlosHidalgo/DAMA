import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedUsersResponse, UserListItem } from '../models/page.model';
import {
  TokenResponse,
  UserCredentials,
  UpdateUsernamePayload,
  UpdateTenantTimezonePayload,
} from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/auth`;

  login(payload: UserCredentials): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/login`, payload);
  }

  registerStudent(payload: UserCredentials): Observable<void> {
    return this.http.post<void>(`${this.base}/register/student`, payload);
  }

  registerTeacher(payload: UserCredentials): Observable<void> {
    return this.http.post<void>(`${this.base}/register/teacher`, payload);
  }

  listStudents(pageIndex = 0): Observable<PagedUsersResponse> {
    return this.http.get<PagedUsersResponse>(`${this.base}/students`, {
      params: new HttpParams().set('pageIndex', pageIndex),
    });
  }

  listTeachers(pageIndex = 0): Observable<PagedUsersResponse> {
    return this.http.get<PagedUsersResponse>(`${this.base}/teachers`, {
      params: new HttpParams().set('pageIndex', pageIndex),
    });
  }

  searchStudentByName(name: string): Observable<UserListItem> {
    return this.http.get<UserListItem>(`${this.base}/students/search`, {
      params: new HttpParams().set('name', name),
    });
  }

  renameUser(userId: string, payload: UpdateUsernamePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${userId}/username`, payload);
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/${userId}`);
  }

  updateTenantTimezone(tenantId: string, payload: UpdateTenantTimezonePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/tenants/${tenantId}/timezone`, payload);
  }
}
