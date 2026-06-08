import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '@env/environment';
import {
  PagedUsersResponse,
  UserListItem,
  TokenResponse,
  RefreshTokenPayload,
  UserCredentials,
  UpdateUsernamePayload,
  UpdateTenantTimezonePayload,
  Tenant,
  CreateTenantPayload,
  UpdateTenantNamePayload,
  TenantTierCount,
} from '@core/models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/auth`;

  login(payload: UserCredentials): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/login`, payload);
  }

  refresh(payload: RefreshTokenPayload): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/refresh`, payload);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.base}/logout`, {});
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

  listTenants(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(`${this.base}/tenants`);
  }

  getTenantTierDistribution(): Observable<TenantTierCount[]> {
    return this.http.get<TenantTierCount[]>(`${this.base}/tenants/tier-distribution`);
  }

  createTenant(payload: CreateTenantPayload): Observable<Tenant> {
    return this.http.post<Tenant>(`${this.base}/tenants`, payload);
  }

  renameTenant(tenantId: string, payload: UpdateTenantNamePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/tenants/${tenantId}/name`, payload);
  }
}
