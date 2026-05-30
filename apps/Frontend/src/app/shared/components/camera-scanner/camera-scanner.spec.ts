import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MockModule } from 'ng-mocks';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { describe, it, expect, beforeEach } from 'vitest';

import { CameraScanner } from './camera-scanner';

describe('CameraScanner', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<CameraScanner>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CameraScanner],
      providers: [provideZonelessChangeDetection()],
    })
      .overrideComponent(CameraScanner, {
        set: { imports: [MockModule(ZXingScannerModule)] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(CameraScanner);
  });

  it('renders the scanner wrapper with the bracket overlays', () => {
    fixture.detectChanges();
    const wrapper = fixture.nativeElement.querySelector('.scanner-wrap');
    expect(wrapper).not.toBeNull();
    expect(wrapper.querySelectorAll('.bracket').length).toBe(4);
  });

  it('emits the scanned payload via the scanned output', () => {
    fixture.detectChanges();
    let emitted: string | undefined;
    fixture.componentInstance.scanned.subscribe((value: string) => (emitted = value));

    fixture.componentInstance.onScan('dama1:payload');

    expect(emitted).toBe('dama1:payload');
  });
});
