import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { CourseColorChip } from './course-color-chip';
import { courseColor } from '@core/utils';

describe('CourseColorChip', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<CourseColorChip>>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseColorChip],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(CourseColorChip);
  });

  function render(): HTMLElement {
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the course name', () => {
    fixture.componentRef.setInput('courseId', 'course-1');
    fixture.componentRef.setInput('name', 'Yoga avanzado');
    expect(render().querySelector('.name')?.textContent).toContain('Yoga avanzado');
  });

  it('derives a non-empty dot background color from courseId', () => {
    fixture.componentRef.setInput('courseId', 'course-42');
    fixture.componentRef.setInput('name', 'X');
    const dot = render().querySelector('.dot') as HTMLElement;
    expect(dot.style.background).not.toBe('');
  });

  it('exposes the computed color() signal matching courseColor()', () => {
    fixture.componentRef.setInput('courseId', 'course-42');
    fixture.componentRef.setInput('name', 'X');
    fixture.detectChanges();
    expect(fixture.componentInstance.color()).toBe(courseColor('course-42'));
  });

  it('updates the color when courseId changes', () => {
    fixture.componentRef.setInput('courseId', 'a');
    fixture.componentRef.setInput('name', 'X');
    fixture.detectChanges();
    const firstColor = (render().querySelector('.dot') as HTMLElement).style.background;

    fixture.componentRef.setInput('courseId', 'b');
    fixture.detectChanges();
    const secondColor = (render().querySelector('.dot') as HTMLElement).style.background;

    expect(firstColor).not.toBe(secondColor);
  });
});
