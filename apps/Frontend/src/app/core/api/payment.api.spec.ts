import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { PaymentApi } from './payment.api';
import { environment } from '@env/environment';

describe('PaymentApi', () => {
  let api: PaymentApi;
  let httpController: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/payment`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(PaymentApi);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('getSummary GETs /summary', () => {
    api.getSummary().subscribe();
    httpController.expectOne(`${base}/summary`).flush({});
  });

  describe('debt templates', () => {
    it('listDebtTemplates GETs /debt-template', () => {
      api.listDebtTemplates().subscribe();
      httpController.expectOne(`${base}/debt-template`).flush([]);
    });

    it('createDebtTemplate POSTs to /debt-template', () => {
      const payload = { description: 'd', classQuantity: 4, cost: 100 };
      api.createDebtTemplate(payload).subscribe();
      const request = httpController.expectOne(`${base}/debt-template`);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual(payload);
      request.flush({});
    });

    it('updateDebtTemplate PUTs to /debt-template/:id', () => {
      const payload = { description: 'd2', classQuantity: 5, cost: 150 };
      api.updateDebtTemplate('t-1', payload).subscribe();
      const request = httpController.expectOne(`${base}/debt-template/t-1`);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual(payload);
      request.flush(null);
    });

    it('deleteDebtTemplate DELETEs /debt-template/:id', () => {
      api.deleteDebtTemplate('t-1').subscribe();
      httpController.expectOne(`${base}/debt-template/t-1`).flush(null);
    });
  });

  describe('qr payments', () => {
    it('createQrDebt POSTs to /qr/:templateId with email body when provided', () => {
      api.createQrDebt('template-1', 'pay@example.com').subscribe();
      const request = httpController.expectOne(`${base}/qr/template-1`);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({ email: 'pay@example.com' });
      request.flush({ identificadorDeuda: 'd-1', status: 'Pending' });
    });

    it('createQrDebt sends an empty body when email is omitted', () => {
      api.createQrDebt('template-1').subscribe();
      const request = httpController.expectOne(`${base}/qr/template-1`);
      expect(request.request.body).toEqual({});
      request.flush({ identificadorDeuda: 'd-1', status: 'Pending' });
    });

    it('createQrDebt sends an empty body when email is null', () => {
      api.createQrDebt('template-1', null).subscribe();
      const request = httpController.expectOne(`${base}/qr/template-1`);
      expect(request.request.body).toEqual({});
      request.flush({ identificadorDeuda: 'd-1', status: 'Pending' });
    });

    it('getQrDebtStatus GETs /qr/:id/status', () => {
      api.getQrDebtStatus('d-1').subscribe();
      httpController
        .expectOne(`${base}/qr/d-1/status`)
        .flush({ identificadorDeuda: 'd-1', status: 'Pending' });
    });

    it.each([
      ['listPendingQr', 'pending'],
      ['listSuccessQr', 'success'],
      ['listFailedQr', 'failed'],
    ] as const)('%s GETs /qr/%s with Index defaulting to 0', (methodName, path) => {
      const callable = api[methodName].bind(api) as (pageIndex?: number) => {
        subscribe(): void;
      };
      callable().subscribe();
      const request = httpController.expectOne((req) => req.url === `${base}/qr/${path}`);
      expect(request.request.params.get('Index')).toBe('0');
      request.flush({ currentIndex: 0, maxIndex: 0, items: [] });
    });

    it('listPendingQr forwards a custom pageIndex', () => {
      api.listPendingQr(7).subscribe();
      const request = httpController.expectOne((req) => req.url === `${base}/qr/pending`);
      expect(request.request.params.get('Index')).toBe('7');
      request.flush({ currentIndex: 7, maxIndex: 10, items: [] });
    });
  });
});
