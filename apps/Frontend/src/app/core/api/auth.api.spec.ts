import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { AuthApi } from './auth.api';
import { environment } from '@env/environment';

describe('AuthApi', () => {
  let api: AuthApi;
  let httpController: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/auth`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(AuthApi);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('login POSTs credentials and returns the token response', () => {
    let received: unknown;
    api.login({ username: 'u', password: 'p' }).subscribe((response) => (received = response));

    const request = httpController.expectOne(`${base}/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ username: 'u', password: 'p' });
    request.flush({ accessToken: 'token-xyz' });

    expect(received).toEqual({ accessToken: 'token-xyz' });
  });

  it.each([
    ['registerStudent', 'register/student'],
    ['registerTeacher', 'register/teacher'],
  ] as const)('%s POSTs credentials to /%s', (method, path) => {
    api[method]({ username: 'u', password: 'p' }).subscribe();
    const request = httpController.expectOne(`${base}/${path}`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ username: 'u', password: 'p' });
    request.flush(null);
  });

  it.each([
    ['listStudents', 'students'],
    ['listTeachers', 'teachers'],
  ] as const)('%s GETs paged users from /%s with default pageIndex=0', (method, path) => {
    api[method]().subscribe();
    const request = httpController.expectOne((req) => req.url === `${base}/${path}`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageIndex')).toBe('0');
    request.flush({ items: [], pageIndex: 0, maxPageIndex: 0 });
  });

  it('listStudents accepts a custom pageIndex', () => {
    api.listStudents(3).subscribe();
    const request = httpController.expectOne((req) => req.url === `${base}/students`);
    expect(request.request.params.get('pageIndex')).toBe('3');
    request.flush({ items: [], pageIndex: 3, maxPageIndex: 5 });
  });

  it('searchStudentByName GETs /students/search with the name query param', () => {
    api.searchStudentByName('alice').subscribe();
    const request = httpController.expectOne((req) => req.url === `${base}/students/search`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('name')).toBe('alice');
    request.flush({ id: 's-1', username: 'alice' });
  });

  it('renameUser PUTs to /users/:id/username', () => {
    api.renameUser('user-1', { username: 'new-name' }).subscribe();
    const request = httpController.expectOne(`${base}/users/user-1/username`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ username: 'new-name' });
    request.flush(null);
  });

  it('deleteUser DELETEs /users/:id', () => {
    api.deleteUser('user-9').subscribe();
    const request = httpController.expectOne(`${base}/users/user-9`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('updateTenantTimezone PUTs to /tenants/:id/timezone', () => {
    api.updateTenantTimezone('tenant-1', { timezone: 'UTC' }).subscribe();
    const request = httpController.expectOne(`${base}/tenants/tenant-1/timezone`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ timezone: 'UTC' });
    request.flush(null);
  });
});
