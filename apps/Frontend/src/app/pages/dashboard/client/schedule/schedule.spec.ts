import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { describe, it, expect, vi } from 'vitest';

import { ScheduleDialog } from './schedule';

const BASE_DATA = {
  mode: 'create' as const,
  kind: 'scheduled' as const,
  courses: [{ id: 'course-1', name: 'Yoga' }],
  teachers: [{ id: 'teacher-1', username: 'Ada' }],
  initial: { courseId: 'course-1' },
};

describe('ScheduleDialog', () => {
  let dialogRef: { close: ReturnType<typeof vi.fn> };

  async function createDialog(data: unknown = BASE_DATA) {
    TestBed.resetTestingModule();
    dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
      imports: [ScheduleDialog],
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();
    return TestBed.createComponent(ScheduleDialog);
  }

  it('closes with the form result when the time range is valid', async () => {
    const fixture = await createDialog();

    fixture.componentInstance.submit();

    expect(dialogRef.close).toHaveBeenCalledWith(
      expect.objectContaining({
        kind: 'scheduled',
        courseId: 'course-1',
        startTime: '09:00',
        endTime: '10:00',
      }),
    );
  });

  it('closes with the configured student limit', async () => {
    const fixture = await createDialog();
    fixture.componentInstance['form'].controls.maxStudentLimit.setValue(30);

    fixture.componentInstance.submit();

    expect(dialogRef.close).toHaveBeenCalledWith(
      expect.objectContaining({ maxStudentLimit: 30 }),
    );
  });

  it('defaults the student limit to 0 (sin límite)', async () => {
    const fixture = await createDialog();

    fixture.componentInstance.submit();

    expect(dialogRef.close).toHaveBeenCalledWith(
      expect.objectContaining({ maxStudentLimit: 0 }),
    );
  });

  it('does not close when the end time is not after the start time', async () => {
    const fixture = await createDialog();
    fixture.componentInstance['form'].controls.endTime.setValue('09:00');

    fixture.componentInstance.submit();

    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('does not close a one-off class without a date', async () => {
    const fixture = await createDialog({ ...BASE_DATA, kind: 'unique' });
    fixture.componentInstance['form'].controls.kind.setValue('unique');
    fixture.componentInstance['form'].controls.date.setValue('');

    fixture.componentInstance.submit();

    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('closes a one-off class once a date is provided', async () => {
    const fixture = await createDialog({ ...BASE_DATA, kind: 'unique' });
    fixture.componentInstance['form'].controls.kind.setValue('unique');
    fixture.componentInstance['form'].controls.date.setValue('2026-06-01');

    fixture.componentInstance.submit();

    expect(dialogRef.close).toHaveBeenCalledWith(
      expect.objectContaining({ kind: 'unique', date: '2026-06-01' }),
    );
  });
});
