import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-placeholder',
  imports: [MatCardModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>{{ title() }}</mat-card-title>
        <mat-card-subtitle>En construcción</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <p>Esta pestaña aún no está implementada.</p>
      </mat-card-content>
    </mat-card>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Placeholder {
  readonly title = input.required<string>();
}
