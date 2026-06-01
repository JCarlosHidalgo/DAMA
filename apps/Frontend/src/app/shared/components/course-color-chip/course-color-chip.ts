import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { courseColor } from '@core/utils';

import { courseColorChipStyles } from './course-color-chip.variants';

@Component({
  selector: 'app-course-color-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './course-color-chip.html',
  host: { class: 'inline-flex' },
})
export class CourseColorChip {
  readonly courseId = input.required<string>();
  readonly name = input.required<string>();
  readonly color = computed(() => courseColor(this.courseId()));
  protected readonly styles = courseColorChipStyles();
}
