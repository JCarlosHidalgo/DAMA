import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { CourseApi } from './course.api';
import { environment } from '@env/environment';

describe('CourseApi', () => {
  let api: CourseApi;
  let httpController: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/course-management/course`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(CourseApi);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('listCourses GETs the base url', () => {
    api.listCourses().subscribe();
    const request = httpController.expectOne(base);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('getCourse GETs /:id', () => {
    api.getCourse('course-1').subscribe();
    const request = httpController.expectOne(`${base}/course-1`);
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('createCourse POSTs to the base url', () => {
    api.createCourse({ name: 'Yoga' }).subscribe();
    const request = httpController.expectOne(base);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Yoga' });
    request.flush({});
  });

  it('updateCourse PUTs to /:id', () => {
    api.updateCourse('course-1', { name: 'Pilates' }).subscribe();
    const request = httpController.expectOne(`${base}/course-1`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Pilates' });
    request.flush(null);
  });

  it('deleteCourse DELETEs /:id', () => {
    api.deleteCourse('course-1').subscribe();
    const request = httpController.expectOne(`${base}/course-1`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  describe('scheduled classes', () => {
    it('createScheduledClass POSTs to /scheduled', () => {
      const payload = {
        courseId: 'c',
        dayOfWeekIndex: 1,
        maxStudentLimit: 30,
        startTime: '08:00',
        endTime: '09:00',
        teachers: [],
      };
      api.createScheduledClass(payload).subscribe();
      const request = httpController.expectOne(`${base}/scheduled`);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual(payload);
      request.flush({});
    });

    it('updateScheduledClass PUTs to /scheduled/:id', () => {
      const payload = {
        dayOfWeekIndex: 2,
        maxStudentLimit: 0,
        startTime: '10:00',
        endTime: '11:00',
        teachers: [],
      };
      api.updateScheduledClass('sch-1', payload).subscribe();
      const request = httpController.expectOne(`${base}/scheduled/sch-1`);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual(payload);
      request.flush(null);
    });

    it('deleteScheduledClass DELETEs /scheduled/:id', () => {
      api.deleteScheduledClass('sch-1').subscribe();
      httpController.expectOne(`${base}/scheduled/sch-1`).flush(null);
    });
  });

  describe('unique classes', () => {
    it('createUniqueClass POSTs to /unique', () => {
      const payload = {
        courseId: 'c',
        date: '2026-05-10',
        maxStudentLimit: 30,
        startTime: '08:00',
        endTime: '09:00',
        teachers: [],
      };
      api.createUniqueClass(payload).subscribe();
      const request = httpController.expectOne(`${base}/unique`);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual(payload);
      request.flush({});
    });

    it('updateUniqueClass PUTs to /unique/:id', () => {
      const payload = {
        date: '2026-05-11',
        maxStudentLimit: 0,
        startTime: '10:00',
        endTime: '11:00',
        teachers: [],
      };
      api.updateUniqueClass('u-1', payload).subscribe();
      const request = httpController.expectOne(`${base}/unique/u-1`);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual(payload);
      request.flush(null);
    });

    it('deleteUniqueClass DELETEs /unique/:id', () => {
      api.deleteUniqueClass('u-1').subscribe();
      httpController.expectOne(`${base}/unique/u-1`).flush(null);
    });
  });

  describe('schedule queries', () => {
    it('getSchedule GETs /schedule with CourseId + WeekPaginationIndex params', () => {
      api.getSchedule('course-1', 2).subscribe();
      const request = httpController.expectOne((req) => req.url === `${base}/schedule`);
      expect(request.request.params.get('CourseId')).toBe('course-1');
      expect(request.request.params.get('WeekPaginationIndex')).toBe('2');
      request.flush({ scheduledClasses: [], uniqueClasses: [] });
    });

    it('getTeacherSchedule GETs /teacher/me with WeekPaginationIndex param', () => {
      api.getTeacherSchedule(-1).subscribe();
      const request = httpController.expectOne((req) => req.url === `${base}/teacher/me`);
      expect(request.request.params.get('WeekPaginationIndex')).toBe('-1');
      request.flush({ scheduledClasses: [], uniqueClasses: [] });
    });

    it('getTenantSchedule GETs /tenant/schedule with WeekPaginationIndex param', () => {
      api.getTenantSchedule(0).subscribe();
      const request = httpController.expectOne((req) => req.url === `${base}/tenant/schedule`);
      expect(request.request.params.get('WeekPaginationIndex')).toBe('0');
      request.flush({ scheduledClasses: [], uniqueClasses: [] });
    });
  });
});
