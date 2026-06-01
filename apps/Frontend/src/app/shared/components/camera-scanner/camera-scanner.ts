import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BarcodeFormat } from '@zxing/library';
import { ZXingScannerModule } from '@zxing/ngx-scanner';

import { cameraScannerStyles } from './camera-scanner.variants';

@Component({
  selector: 'app-camera-scanner',
  imports: [ZXingScannerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './camera-scanner.html',
  host: { class: 'block' },
})
export class CameraScanner {
  readonly enabled = input<boolean>(true);
  readonly scanned = output<string>();

  protected readonly styles = cameraScannerStyles();
  protected readonly formats: BarcodeFormat[] = [BarcodeFormat.QR_CODE];

  onScan(value: string): void {
    this.scanned.emit(value);
  }
}
