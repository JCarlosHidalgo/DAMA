import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import { ConfirmDialogData, ConfirmationDialog } from './confirmation-dialog';

describe('ConfirmationDialog', () => {
  let dialogRefMock: { close: ReturnType<typeof vi.fn> };

  function configureWithData(data: ConfirmDialogData) {
    dialogRefMock = { close: vi.fn() };
    TestBed.configureTestingModule({
      imports: [ConfirmationDialog],
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatDialogRef, useValue: dialogRefMock },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    });
    const fixture = TestBed.createComponent(ConfirmationDialog);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the provided title and message', () => {
    const fixture = configureWithData({
      title: 'Confirma',
      message: '¿Quieres continuar?',
    });
    const host: HTMLElement = fixture.nativeElement;

    expect(host.querySelector('h2')?.textContent).toContain('Confirma');
    expect(host.querySelector('p')?.textContent).toContain('¿Quieres continuar?');
  });

  it('uses default button labels when none are provided', () => {
    const fixture = configureWithData({
      title: 't',
      message: 'm',
    });
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');

    expect(buttons[0].textContent).toContain('Cancelar');
    expect(buttons[1].textContent).toContain('Confirmar');
  });

  it('uses the provided custom labels', () => {
    const fixture = configureWithData({
      title: 't',
      message: 'm',
      confirmLabel: 'Sí, eliminar',
      cancelLabel: 'No',
    });
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');

    expect(buttons[0].textContent).toContain('No');
    expect(buttons[1].textContent).toContain('Sí, eliminar');
  });

  it('closes with false when the cancel button is clicked', () => {
    const fixture = configureWithData({ title: 't', message: 'm' });
    const cancelButton = (fixture.nativeElement as HTMLElement).querySelectorAll('button')[0];

    (cancelButton as HTMLButtonElement).click();

    expect(dialogRefMock.close).toHaveBeenCalledWith(false);
  });

  it('closes with true when the confirm button is clicked', () => {
    const fixture = configureWithData({ title: 't', message: 'm' });
    const confirmButton = (fixture.nativeElement as HTMLElement).querySelectorAll('button')[1];

    (confirmButton as HTMLButtonElement).click();

    expect(dialogRefMock.close).toHaveBeenCalledWith(true);
  });

  it('marks the confirm button as destructive when data.destructive is true', () => {
    const fixture = configureWithData({
      title: 't',
      message: 'm',
      destructive: true,
    });
    const confirmButton = (fixture.nativeElement as HTMLElement).querySelectorAll('button')[1];

    expect(
      confirmButton.classList.contains('[--mdc-filled-button-container-color:var(--dama-danger)]'),
    ).toBe(true);
  });

  it('does not mark the confirm button as destructive by default', () => {
    const fixture = configureWithData({ title: 't', message: 'm' });
    const confirmButton = (fixture.nativeElement as HTMLElement).querySelectorAll('button')[1];

    expect(
      confirmButton.classList.contains('[--mdc-filled-button-container-color:var(--dama-danger)]'),
    ).toBe(false);
  });
});
