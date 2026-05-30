import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import { NotificationService } from './notification-service';

describe('NotificationService', () => {
  let openSpy: ReturnType<typeof vi.fn>;
  let service: NotificationService;

  beforeEach(() => {
    openSpy = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: MatSnackBar, useValue: { open: openSpy } }],
    });
    service = TestBed.inject(NotificationService);
  });

  it('success opens the snackbar with the success panel class and default duration', () => {
    service.success('Saved');

    expect(openSpy).toHaveBeenCalledTimes(1);
    const [message, action, config] = openSpy.mock.calls[0];
    expect(message).toBe('Saved');
    expect(action).toBe('OK');
    expect(config.duration).toBe(4000);
    expect(config.panelClass).toBe('dama-snack-success');
  });

  it('error opens the snackbar with the error panel class', () => {
    service.error('Boom');
    const [, , config] = openSpy.mock.calls[0];
    expect(config.panelClass).toBe('dama-snack-error');
  });

  it('info opens the snackbar with the default duration and no panel class override', () => {
    service.info('FYI');
    const [, , config] = openSpy.mock.calls[0];
    expect(config.duration).toBe(4000);
    expect(config.panelClass).toBeUndefined();
  });

  it('allows callers to override duration via options', () => {
    service.success('Hi', { duration: 10000 });
    const [, , config] = openSpy.mock.calls[0];
    expect(config.duration).toBe(10000);
    expect(config.panelClass).toBe('dama-snack-success');
  });
});
