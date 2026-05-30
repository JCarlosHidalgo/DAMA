import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Page } from '../models/page.model';
import {
  PaymentSummary,
  DebtTemplate,
  CreateDebtTemplatePayload,
  UpdateDebtTemplatePayload,
  PendingQrPayment,
  SuccessQrPayment,
  FailedQrPayment,
  QrDebtPending,
  QrDebtStatus,
} from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/payment`;

  getSummary(): Observable<PaymentSummary> {
    return this.http.get<PaymentSummary>(`${this.base}/summary`);
  }

  listDebtTemplates(): Observable<DebtTemplate[]> {
    return this.http.get<DebtTemplate[]>(`${this.base}/debt-template`);
  }

  createDebtTemplate(payload: CreateDebtTemplatePayload): Observable<DebtTemplate> {
    return this.http.post<DebtTemplate>(`${this.base}/debt-template`, payload);
  }

  updateDebtTemplate(id: string, payload: UpdateDebtTemplatePayload): Observable<void> {
    return this.http.put<void>(`${this.base}/debt-template/${id}`, payload);
  }

  deleteDebtTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/debt-template/${id}`);
  }

  createQrDebt(templateId: string, email?: string | null): Observable<QrDebtPending> {
    const body = email ? { email } : {};
    return this.http.post<QrDebtPending>(`${this.base}/qr/${templateId}`, body);
  }

  getQrDebtStatus(id: string): Observable<QrDebtStatus> {
    return this.http.get<QrDebtStatus>(`${this.base}/qr/${id}/status`);
  }

  listPendingQr(pageIndex = 0): Observable<Page<PendingQrPayment>> {
    return this.http.get<Page<PendingQrPayment>>(`${this.base}/qr/pending`, {
      params: new HttpParams().set('Index', pageIndex),
    });
  }

  listSuccessQr(pageIndex = 0): Observable<Page<SuccessQrPayment>> {
    return this.http.get<Page<SuccessQrPayment>>(`${this.base}/qr/success`, {
      params: new HttpParams().set('Index', pageIndex),
    });
  }

  listFailedQr(pageIndex = 0): Observable<Page<FailedQrPayment>> {
    return this.http.get<Page<FailedQrPayment>>(`${this.base}/qr/failed`, {
      params: new HttpParams().set('Index', pageIndex),
    });
  }
}
