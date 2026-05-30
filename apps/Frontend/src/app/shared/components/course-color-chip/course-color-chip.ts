import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { courseColor } from '@core/utils';

@Component({
  selector: 'app-course-color-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './course-color-chip.html',
  styleUrl: './course-color-chip.scss',
})
export class CourseColorChip {
  readonly courseId = input.required<string>();
  readonly name = input.required<string>();
  readonly color = computed(() => courseColor(this.courseId()));
}
