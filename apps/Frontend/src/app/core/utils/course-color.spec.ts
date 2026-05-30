import { describe, it, expect } from 'vitest';

import { courseColor } from './course-color';

describe('courseColor', () => {
  it('returns an hsl string with saturation 65% and lightness 55%', () => {
    expect(courseColor('any-id')).toMatch(/^hsl\(\d+, 65%, 55%\)$/);
  });

  it('is deterministic for the same input', () => {
    expect(courseColor('course-abc')).toBe(courseColor('course-abc'));
  });

  it('produces different hues for different inputs', () => {
    const colorOne = courseColor('course-alpha');
    const colorTwo = courseColor('course-beta');
    expect(colorOne).not.toBe(colorTwo);
  });

  it('returns a fallback hsl for empty string input', () => {
    expect(courseColor('')).toBe('hsl(0, 65%, 55%)');
  });

  it('keeps the hue inside the [0, 360) range for arbitrary inputs', () => {
    const colors = ['a', 'long-course-id-with-many-chars', 'áéí', '12345'];
    for (const id of colors) {
      const match = courseColor(id).match(/^hsl\((\d+), 65%, 55%\)$/);
      expect(match).not.toBeNull();
      const hue = Number(match![1]);
      expect(hue).toBeGreaterThanOrEqual(0);
      expect(hue).toBeLessThan(360);
    }
  });
});
