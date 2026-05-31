import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { CredentialsApi } from './credentials.api';
import { environment } from '@env/environment';

describe('CredentialsApi', () => {
  let api: CredentialsApi;
  let httpController: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/credentials`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(CredentialsApi);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it.each([
    ['clientEcho', 'client-credentials'],
    ['teacherEcho', 'teacher-credentials'],
    ['studentEcho', 'student-credentials'],
  ] as const)('%s issues a GET to /%s', (method, path) => {
    let body: unknown;
    api[method]().subscribe((response) => {
      body = response;
    });
    const request = httpController.expectOne(`${base}/${path}`);
    expect(request.request.method).toBe('GET');
    request.flush({ ok: true });
    expect(body).toEqual({ ok: true });
  });
});
