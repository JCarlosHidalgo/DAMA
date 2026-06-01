import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import { AttendanceMarkedDialog } from './attendance-marked-dialog';

describe('AttendanceMarkedDialog', () => {
  let dialogRefMock: { close: ReturnType<typeof vi.fn> };

  function configure() {
    dialogRefMock = { close: vi.fn() };
    TestBed.configureTestingModule({
      imports: [AttendanceMarkedDialog],
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatDialogRef, useValue: dialogRefMock },
      ],
    });
    const fixture = TestBed.createComponent(AttendanceMarkedDialog);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the already-marked message and a check icon', () => {
    const fixture = configure();
    const host: HTMLElement = fixture.nativeElement;

    expect(host.querySelector('p')?.textContent).toContain(
      'Ya marcaste asistencia para esta clase',
    );
    expect(host.querySelector('app-icon')).not.toBeNull();
  });

  it('closes the dialog when the acknowledge button is clicked', () => {
    const fixture = configure();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button');

    (button as HTMLButtonElement).click();

    expect(dialogRefMock.close).toHaveBeenCalled();
  });
});
