import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CredentialsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/credentials`;

  clientEcho(): Observable<unknown> {
    return this.http.get<unknown>(`${this.base}/client-credentials`);
  }

  teacherEcho(): Observable<unknown> {
    return this.http.get<unknown>(`${this.base}/teacher-credentials`);
  }

  studentEcho(): Observable<unknown> {
    return this.http.get<unknown>(`${this.base}/student-credentials`);
  }
}
