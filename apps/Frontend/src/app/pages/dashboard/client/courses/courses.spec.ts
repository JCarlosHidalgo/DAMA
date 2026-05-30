import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { describe, it, expect, vi } from 'vitest';

import { CourseDialog } from './courses';

describe('CourseDialog', () => {
  async function createDialog(data: { mode: 'create' | 'edit'; name: string }) {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [CourseDialog],
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();
    return TestBed.createComponent(CourseDialog);
  }

  it('is invalid when the name is empty', async () => {
    const fixture = await createDialog({ mode: 'create', name: '' });
    expect(fixture.componentInstance.form.controls.name.hasError('required')).toBe(true);
  });

  it('is valid for a plain name', async () => {
    const fixture = await createDialog({ mode: 'create', name: 'Yoga' });
    expect(fixture.componentInstance.form.valid).toBe(true);
  });

  it('rejects a name containing a dot', async () => {
    const fixture = await createDialog({ mode: 'create', name: 'Yoga 2.0' });
    expect(fixture.componentInstance.form.controls.name.hasError('hasDot')).toBe(true);
  });

  it('rejects a name longer than 128 characters', async () => {
    const fixture = await createDialog({ mode: 'create', name: 'a'.repeat(129) });
    expect(fixture.componentInstance.form.controls.name.hasError('maxlength')).toBe(true);
  });

  it('prefills the name in edit mode', async () => {
    const fixture = await createDialog({ mode: 'edit', name: 'Pilates' });
    expect(fixture.componentInstance.form.controls.name.value).toBe('Pilates');
  });
});
