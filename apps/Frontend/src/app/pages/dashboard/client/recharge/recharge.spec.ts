import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { Recharge } from './recharge';
import { AttendanceApi, AuthApi } from '@core/api';
import { DialogService, NotificationService } from '@core/services';

const STUDENT = { id: 'student-1', username: 'Ada Lovelace' };

describe('Recharge', () => {
  let authApi: { searchStudentByName: ReturnType<typeof vi.fn> };
  let attendanceApi: {
    clientIncrementStudent: ReturnType<typeof vi.fn>;
    clientIncrementTenant: ReturnType<typeof vi.fn>;
  };
  let dialogs: { confirm: ReturnType<typeof vi.fn> };
  let notifications: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  async function setUp() {
    TestBed.resetTestingModule();
    authApi = { searchStudentByName: vi.fn(() => of(STUDENT)) };
    attendanceApi = {
      clientIncrementStudent: vi.fn(() => of(undefined)),
      clientIncrementTenant: vi.fn(() => of({ affected: 7 })),
    };
    dialogs = { confirm: vi.fn(() => Promise.resolve(true)) };
    notifications = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [Recharge],
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthApi, useValue: authApi },
        { provide: AttendanceApi, useValue: attendanceApi },
        { provide: DialogService, useValue: dialogs },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    return TestBed.createComponent(Recharge);
  }

  describe('onSubmitStudent', () => {
    it('does nothing while the form is invalid', async () => {
      const fixture = await setUp();
      fixture.componentInstance['studentForm'].setValue({ name: '', quantity: 1 });

      await fixture.componentInstance.onSubmitStudent();

      expect(authApi.searchStudentByName).not.toHaveBeenCalled();
    });

    it('reports when the student cannot be found and does not increment', async () => {
      const fixture = await setUp();
      authApi.searchStudentByName.mockReturnValue(throwError(() => new Error('404')));
      fixture.componentInstance['studentForm'].setValue({ name: 'Ada Lovelace', quantity: 3 });

      await fixture.componentInstance.onSubmitStudent();

      expect(notifications.error).toHaveBeenCalledWith(
        'No se encontró un estudiante con ese nombre.',
      );
      expect(attendanceApi.clientIncrementStudent).not.toHaveBeenCalled();
      expect(fixture.componentInstance['studentBusy']()).toBe(false);
    });

    it('does not increment when the confirmation is rejected', async () => {
      const fixture = await setUp();
      dialogs.confirm.mockResolvedValue(false);
      fixture.componentInstance['studentForm'].setValue({ name: 'Ada Lovelace', quantity: 3 });

      await fixture.componentInstance.onSubmitStudent();

      expect(attendanceApi.clientIncrementStudent).not.toHaveBeenCalled();
    });

    it('increments the found student and resets the form on success', async () => {
      const fixture = await setUp();
      fixture.componentInstance['studentForm'].setValue({ name: 'Ada Lovelace', quantity: 3 });

      await fixture.componentInstance.onSubmitStudent();

      expect(attendanceApi.clientIncrementStudent).toHaveBeenCalledWith(
        'student-1',
        expect.objectContaining({ quantity: 3, studentName: 'Ada Lovelace' }),
      );
      expect(notifications.success).toHaveBeenCalled();
      expect(fixture.componentInstance['studentForm'].getRawValue()).toEqual({
        name: '',
        quantity: 1,
      });
      expect(fixture.componentInstance['studentBusy']()).toBe(false);
    });

    it('notifies and resets busy when the increment fails', async () => {
      const fixture = await setUp();
      attendanceApi.clientIncrementStudent.mockReturnValue(throwError(() => new Error('boom')));
      fixture.componentInstance['studentForm'].setValue({ name: 'Ada Lovelace', quantity: 3 });

      await fixture.componentInstance.onSubmitStudent();

      expect(notifications.error).toHaveBeenCalledWith('Error al recargar.');
      expect(fixture.componentInstance['studentBusy']()).toBe(false);
    });
  });

  describe('onSubmitTenant', () => {
    it('does not run a mass recharge unless confirmed', async () => {
      const fixture = await setUp();
      dialogs.confirm.mockResolvedValue(false);

      await fixture.componentInstance.onSubmitTenant();

      expect(attendanceApi.clientIncrementTenant).not.toHaveBeenCalled();
    });

    it('runs the mass recharge and reports the affected count on success', async () => {
      const fixture = await setUp();
      fixture.componentInstance['tenantForm'].setValue({ quantity: 5 });

      await fixture.componentInstance.onSubmitTenant();

      expect(attendanceApi.clientIncrementTenant).toHaveBeenCalledWith(
        expect.objectContaining({ quantity: 5 }),
      );
      expect(notifications.success).toHaveBeenCalledWith('Actualizados 7 estudiantes.');
      expect(fixture.componentInstance['tenantBusy']()).toBe(false);
    });

    it('notifies and resets busy when the mass recharge fails', async () => {
      const fixture = await setUp();
      attendanceApi.clientIncrementTenant.mockReturnValue(throwError(() => new Error('boom')));

      await fixture.componentInstance.onSubmitTenant();

      expect(notifications.error).toHaveBeenCalledWith('Error al recargar.');
      expect(fixture.componentInstance['tenantBusy']()).toBe(false);
    });
  });
});
