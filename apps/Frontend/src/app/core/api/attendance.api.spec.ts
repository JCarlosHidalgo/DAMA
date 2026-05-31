import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { AttendanceApi } from './attendance.api';
import { environment } from '@env/environment';

describe('AttendanceApi', () => {
  let api: AttendanceApi;
  let httpController: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/attendance`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(AttendanceApi);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('getMyRemain GETs /remain/me', () => {
    api.getMyRemain().subscribe();
    httpController.expectOne(`${base}/remain/me`).flush({});
  });

  it('getStudentRemain GETs /remain/:id', () => {
    api.getStudentRemain('student-1').subscribe();
    httpController.expectOne(`${base}/remain/student-1`).flush({});
  });

  it('clientIncrementStudent POSTs to /remain/client/:id', () => {
    api
      .clientIncrementStudent('s-1', {
        requestId: 'req-1',
        quantity: 5,
        studentName: 'Ana',
      })
      .subscribe();
    const request = httpController.expectOne(`${base}/remain/client/s-1`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ requestId: 'req-1', quantity: 5, studentName: 'Ana' });
    request.flush(null);
  });

  it('clientIncrementTenant POSTs to /remain/client/tenant and returns affected count', () => {
    let response: { affected: number } | undefined;
    api.clientIncrementTenant({ requestId: 'req-2', quantity: 2 }).subscribe((r) => (response = r));
    const request = httpController.expectOne(`${base}/remain/client/tenant`);
    expect(request.request.method).toBe('POST');
    request.flush({ affected: 17 });
    expect(response).toEqual({ affected: 17 });
  });

  it.each([
    ['markScheduled', 'attendance/scheduled'],
    ['markUnique', 'attendance/unique'],
  ] as const)('%s POSTs to /%s', (method, path) => {
    api[method]({ classId: 'c-1', courseName: 'Yoga' }).subscribe();
    const request = httpController.expectOne(`${base}/${path}`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ classId: 'c-1', courseName: 'Yoga' });
    request.flush(null);
  });

  it('scheduledRoster GETs /attendance/scheduled/class/:id/:date', () => {
    api.scheduledRoster('c-1', '2026-05-10').subscribe();
    httpController.expectOne(`${base}/attendance/scheduled/class/c-1/2026-05-10`).flush([]);
  });

  it('uniqueRoster GETs /attendance/unique/class/:id', () => {
    api.uniqueRoster('c-1').subscribe();
    httpController.expectOne(`${base}/attendance/unique/class/c-1`).flush([]);
  });

  it.each([
    ['myScheduledHistory', 'attendance/scheduled/student'],
    ['myUniqueHistory', 'attendance/unique/student'],
  ] as const)('%s GETs /%s/:studentId', (method, path) => {
    api[method]('student-7').subscribe();
    httpController.expectOne(`${base}/${path}/student-7`).flush([]);
  });

  it.each([
    ['listMyScheduledAttendance', 'attendance/scheduled/me'],
    ['listMyUniqueAttendance', 'attendance/unique/me'],
  ] as const)('%s GETs /%s with Index param defaulting to 0', (method, path) => {
    api[method]().subscribe();
    const request = httpController.expectOne((req) => req.url === `${base}/${path}`);
    expect(request.request.params.get('Index')).toBe('0');
    request.flush({ currentIndex: 0, maxIndex: 0, items: [] });
  });

  it('listMyScheduledAttendance forwards a custom pageIndex', () => {
    api.listMyScheduledAttendance(4).subscribe();
    const request = httpController.expectOne(
      (req) => req.url === `${base}/attendance/scheduled/me`,
    );
    expect(request.request.params.get('Index')).toBe('4');
    request.flush({ currentIndex: 4, maxIndex: 8, items: [] });
  });
});
