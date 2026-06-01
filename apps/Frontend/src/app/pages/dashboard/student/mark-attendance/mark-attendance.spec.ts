import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { MarkAttendance } from './mark-attendance';
import { AttendanceApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationService } from '@core/services';
import { ClassKindStrategies } from '@core/strategies';
import { encodeQr, todayDateOnlyInTenant } from '@core/utils';
import { buildJwtClaims } from '@testing';

interface ScenarioOverrides {
  remainResult?: ReturnType<typeof of> | ReturnType<typeof throwError>;
  markResult?: ReturnType<typeof of> | ReturnType<typeof throwError>;
  tenantId?: string;
  scheduledHistory?: { classId: string; classDate: string }[];
  uniqueHistory?: { classId: string }[];
}

describe('MarkAttendance', () => {
  let attendanceStub: {
    getMyRemain: ReturnType<typeof vi.fn>;
    markScheduled: ReturnType<typeof vi.fn>;
    markUnique: ReturnType<typeof vi.fn>;
    myScheduledHistory: ReturnType<typeof vi.fn>;
    myUniqueHistory: ReturnType<typeof vi.fn>;
  };
  let notificationsStub: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let scheduledStrategy: { markAttendance: ReturnType<typeof vi.fn> };
  let uniqueStrategy: { markAttendance: ReturnType<typeof vi.fn> };
  let strategiesStub: { for: ReturnType<typeof vi.fn> };
  let dialogStub: { open: ReturnType<typeof vi.fn> };
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.spyOn>;

  async function setUp(overrides: ScenarioOverrides = {}) {
    TestBed.resetTestingModule();
    scheduledStrategy = { markAttendance: vi.fn(() => of(undefined)) };
    uniqueStrategy = { markAttendance: vi.fn(() => of(undefined)) };
    strategiesStub = {
      for: vi.fn((kind: string) => (kind === 'Scheduled' ? scheduledStrategy : uniqueStrategy)),
    };
    attendanceStub = {
      getMyRemain: vi.fn(() => overrides.remainResult ?? of({ numberOfClasses: 5 })),
      markScheduled: vi.fn(() => of(undefined)),
      markUnique: vi.fn(() => of(undefined)),
      myScheduledHistory: vi.fn(() => of(overrides.scheduledHistory ?? [])),
      myUniqueHistory: vi.fn(() => of(overrides.uniqueHistory ?? [])),
    };
    notificationsStub = { success: vi.fn(), error: vi.fn() };
    dialogStub = { open: vi.fn(() => ({ afterClosed: () => of(undefined) })) };

    await TestBed.configureTestingModule({
      imports: [MarkAttendance],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AttendanceApi, useValue: attendanceStub },
        {
          provide: AuthService,
          useValue: {
            claims: signal(buildJwtClaims({ tenantId: overrides.tenantId ?? 'tenant-1' })),
            tenantTimezone: signal('America/La_Paz'),
          },
        },
        { provide: NotificationService, useValue: notificationsStub },
        { provide: ClassKindStrategies, useValue: strategiesStub },
        { provide: MatDialog, useValue: dialogStub },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(MarkAttendance);
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    return fixture;
  }

  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function flushMicrotasks(): Promise<void> {
    await new Promise<void>((resolve) => Promise.resolve().then(() => resolve()));
  }

  it('loads the remain count on construction', async () => {
    const fixture = await setUp();
    await flushMicrotasks();
    expect(attendanceStub.getMyRemain).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance['remain']()).toBe(5);
  });

  it('sets remain to null when the remain request fails', async () => {
    const fixture = await setUp({ remainResult: throwError(() => new Error('x')) });
    await flushMicrotasks();
    expect(fixture.componentInstance['remain']()).toBeNull();
  });

  describe('onScan', () => {
    it('rejects an unparseable QR with a "no válido" error', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      await fixture.componentInstance.onScan('garbage');

      expect(fixture.componentInstance['state']()).toBe('error');
      expect(fixture.componentInstance['errorMessage']()).toContain('no válido');
      expect(strategiesStub.for).not.toHaveBeenCalled();
    });

    it('rejects a QR from a different tenant', async () => {
      const fixture = await setUp({ tenantId: 'tenant-1' });
      await flushMicrotasks();
      const otherTenantQr = encodeQr({
        tenantId: 'tenant-OTHER',
        courseName: 'X',
        kind: 'SCHEDULED',
        classId: 'c',
      });
      await fixture.componentInstance.onScan(otherTenantQr);

      expect(fixture.componentInstance['state']()).toBe('error');
      expect(fixture.componentInstance['errorMessage']()).toContain('no corresponde');
    });

    it('marks attendance via the Scheduled strategy on a valid Scheduled QR', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'Yoga',
        kind: 'SCHEDULED',
        classId: 'class-1',
      });

      await fixture.componentInstance.onScan(qr);

      expect(strategiesStub.for).toHaveBeenCalledWith('Scheduled');
      expect(scheduledStrategy.markAttendance).toHaveBeenCalledWith({
        classId: 'class-1',
        courseName: 'Yoga',
      });
      expect(fixture.componentInstance['state']()).toBe('success');
      expect(notificationsStub.success).toHaveBeenCalledWith(
        'Asistencia registrada.',
        expect.objectContaining({ duration: 3000 }),
      );

      vi.advanceTimersByTime(1300);
      expect(navigateSpy).toHaveBeenCalledWith('/yo/resumen');
    });

    it('uses the Unique strategy on a UNIQUE QR', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'Yoga',
        kind: 'UNIQUE',
        classId: 'class-7',
      });

      await fixture.componentInstance.onScan(qr);

      expect(strategiesStub.for).toHaveBeenCalledWith('Unique');
      expect(uniqueStrategy.markAttendance).toHaveBeenCalled();
    });

    it('shows the outside-window message when the strategy throws OutsideAllowedWindow', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      scheduledStrategy.markAttendance.mockReturnValueOnce(
        throwError(() => new Error('OutsideAllowedWindow')),
      );
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'X',
        kind: 'SCHEDULED',
        classId: 'c',
      });
      await fixture.componentInstance.onScan(qr);

      expect(fixture.componentInstance['state']()).toBe('error');
      expect(fixture.componentInstance['errorMessage']()).toContain('Fuera del horario');
    });

    it('shows a generic error when the strategy throws something else', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      scheduledStrategy.markAttendance.mockReturnValueOnce(throwError(() => new Error('boom')));
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'X',
        kind: 'SCHEDULED',
        classId: 'c',
      });
      await fixture.componentInstance.onScan(qr);

      expect(fixture.componentInstance['errorMessage']()).toContain('No se pudo registrar');
    });

    it('shows the already-marked dialog without marking when the Scheduled class was already marked', async () => {
      const today = todayDateOnlyInTenant('America/La_Paz');
      const fixture = await setUp({ scheduledHistory: [{ classId: 'class-1', classDate: today }] });
      await flushMicrotasks();
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'Yoga',
        kind: 'SCHEDULED',
        classId: 'class-1',
      });

      await fixture.componentInstance.onScan(qr);

      expect(dialogStub.open).toHaveBeenCalledTimes(1);
      expect(scheduledStrategy.markAttendance).not.toHaveBeenCalled();
    });

    it('shows the already-marked dialog without marking when the Unique class was already marked', async () => {
      const fixture = await setUp({ uniqueHistory: [{ classId: 'class-7' }] });
      await flushMicrotasks();
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'Yoga',
        kind: 'UNIQUE',
        classId: 'class-7',
      });

      await fixture.componentInstance.onScan(qr);

      expect(dialogStub.open).toHaveBeenCalledTimes(1);
      expect(uniqueStrategy.markAttendance).not.toHaveBeenCalled();
    });

    it('shows the already-marked dialog when the strategy throws AlreadyMarked', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      scheduledStrategy.markAttendance.mockReturnValueOnce(
        throwError(() => new Error('AlreadyMarked')),
      );
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'X',
        kind: 'SCHEDULED',
        classId: 'c',
      });

      await fixture.componentInstance.onScan(qr);

      expect(dialogStub.open).toHaveBeenCalledTimes(1);
      expect(fixture.componentInstance['state']()).toBe('idle');
    });

    it('ignores scans while a previous scan is in flight', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      fixture.componentInstance['state'].set('submitting');
      const qr = encodeQr({
        tenantId: 'tenant-1',
        courseName: 'X',
        kind: 'SCHEDULED',
        classId: 'c',
      });
      await fixture.componentInstance.onScan(qr);

      expect(strategiesStub.for).not.toHaveBeenCalled();
    });
  });

  describe('reset', () => {
    it('clears state and re-enables the scanner', async () => {
      const fixture = await setUp();
      await flushMicrotasks();
      fixture.componentInstance['errorMessage'].set('previous');
      fixture.componentInstance['state'].set('error');
      fixture.componentInstance['scannerEnabled'].set(false);

      fixture.componentInstance.reset();

      expect(fixture.componentInstance['state']()).toBe('idle');
      expect(fixture.componentInstance['errorMessage']()).toBe('');
      expect(fixture.componentInstance['scannerEnabled']()).toBe(true);
    });
  });
});
