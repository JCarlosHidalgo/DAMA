import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BarcodeFormat } from '@zxing/library';
import { ZXingScannerModule } from '@zxing/ngx-scanner';

@Component({
  selector: 'app-camera-scanner',
  imports: [ZXingScannerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './camera-scanner.html',
  styleUrl: './camera-scanner.scss',
})
export class CameraScanner {
  readonly enabled = input<boolean>(true);
  readonly scanned = output<string>();

  protected readonly formats: BarcodeFormat[] = [BarcodeFormat.QR_CODE];

  onScan(value: string): void {
    this.scanned.emit(value);
  }
}
