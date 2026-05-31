import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';

import { DialogService } from './dialog-service';
import { ConfirmationDialog } from '@core/utils';

describe('DialogService', () => {
  let openSpy: ReturnType<typeof vi.fn>;
  let service: DialogService;

  function configure(afterClosedValue: unknown): void {
    openSpy = vi.fn(() => ({
      afterClosed: () => of(afterClosedValue),
    }));
    TestBed.configureTestingModule({
      providers: [{ provide: MatDialog, useValue: { open: openSpy } }],
    });
    service = TestBed.inject(DialogService);
  }

  describe('confirm', () => {
    it('opens ConfirmationDialog with the data + default width and returns the dialog result', async () => {
      configure(true);

      const accepted = await service.confirm({ title: 't', message: 'm' });

      expect(openSpy).toHaveBeenCalledWith(ConfirmationDialog, {
        data: { title: 't', message: 'm' },
        width: '420px',
      });
      expect(accepted).toBe(true);
    });

    it('returns false when the dialog closes with undefined', async () => {
      configure(undefined);

      const accepted = await service.confirm({ title: 't', message: 'm' });

      expect(accepted).toBe(false);
    });

    it('returns false when the dialog closes with false', async () => {
      configure(false);
      expect(await service.confirm({ title: 't', message: 'm' })).toBe(false);
    });
  });

  describe('openForm', () => {
    class FakeFormComponent {}

    it('opens the given component with the data + config and returns its result', async () => {
      configure({ saved: true });

      const result = await service.openForm<FakeFormComponent, { id: string }, { saved: boolean }>(
        FakeFormComponent,
        { id: 'x' },
        { width: '600px' },
      );

      expect(openSpy).toHaveBeenCalledWith(FakeFormComponent, {
        data: { id: 'x' },
        width: '600px',
      });
      expect(result).toEqual({ saved: true });
    });

    it('returns undefined when the dialog closes with no value', async () => {
      configure(undefined);

      const result = await service.openForm<FakeFormComponent, { id: string }, { saved: boolean }>(
        FakeFormComponent,
        { id: 'x' },
      );

      expect(result).toBeUndefined();
    });
  });
});
