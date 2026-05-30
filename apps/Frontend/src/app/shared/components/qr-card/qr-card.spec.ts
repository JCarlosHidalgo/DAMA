import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MockComponent } from 'ng-mocks';
import { QRCodeComponent } from 'angularx-qrcode';
import { describe, it, expect, beforeEach } from 'vitest';

import { QrCard } from './qr-card';

describe('QrCard', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<QrCard>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QrCard],
      providers: [provideZonelessChangeDetection()],
    })
      .overrideComponent(QrCard, {
        set: { imports: [MockComponent(QRCodeComponent)] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(QrCard);
  });

  function render(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the qrcode component with the payload as qrdata', () => {
    fixture.componentRef.setInput('payload', 'dama1:hello');
    const qrcode = render().querySelector('qrcode') as HTMLElement;
    expect(qrcode).not.toBeNull();
  });

  it('does not render the meta block when both title and subtitle are null', () => {
    fixture.componentRef.setInput('payload', 'p');
    expect(render().querySelector('.meta')).toBeNull();
  });

  it('renders title block when provided', () => {
    fixture.componentRef.setInput('payload', 'p');
    fixture.componentRef.setInput('title', 'Mi QR');
    const titleBlock = render().querySelector('.t-h2');
    expect(titleBlock?.textContent).toContain('Mi QR');
  });

  it('renders subtitle block when provided', () => {
    fixture.componentRef.setInput('payload', 'p');
    fixture.componentRef.setInput('subtitle', 'Sub');
    expect(render().querySelector('.t-small')?.textContent).toContain('Sub');
  });
});
