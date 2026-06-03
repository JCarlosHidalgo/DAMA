import { describe, it, expect } from 'vitest';

import { coursesSubtitle, resolveCourseCreate, resolveCourseEdit } from './courses.logic';

describe('resolveCourseCreate', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveCourseCreate(undefined)).toEqual({ kind: 'skip' });
  });

  it('creates with the provided name', () => {
    expect(resolveCourseCreate('Yoga')).toEqual({ kind: 'create', name: 'Yoga' });
  });
});

describe('resolveCourseEdit', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveCourseEdit('Yoga', undefined)).toEqual({ kind: 'skip' });
  });

  it('skips when the name is unchanged', () => {
    expect(resolveCourseEdit('Yoga', 'Yoga')).toEqual({ kind: 'skip' });
  });

  it('updates when the name changed', () => {
    expect(resolveCourseEdit('Yoga', 'Pilates')).toEqual({ kind: 'update', name: 'Pilates' });
  });
});

describe('coursesSubtitle', () => {
  it('renders the count label', () => {
    expect(coursesSubtitle(3)).toBe('3 curso(s)');
  });
});
