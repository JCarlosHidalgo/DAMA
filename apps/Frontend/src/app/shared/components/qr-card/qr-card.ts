import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { QRCodeComponent } from 'angularx-qrcode';

@Component({
  selector: 'app-qr-card',
  imports: [QRCodeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './qr-card.html',
  styleUrl: './qr-card.scss',
})
export class QrCard {
  readonly payload = input.required<string>();
  readonly title = input<string | null>(null);
  readonly subtitle = input<string | null>(null);
  readonly size = input<number>(280);
}
