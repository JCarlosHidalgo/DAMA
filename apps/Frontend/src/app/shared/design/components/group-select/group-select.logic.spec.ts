import { describe, expect, it } from 'vitest';

import { ClassGroup } from '@core/models';

import {
  GROUPS_QUERY_KEY,
  TEACHER_GROUPS_QUERY_KEY,
  findSelectedGroup,
  groupsQueryKey,
  resolveGroupCreate,
  resolveGroupRename,
} from './group-select.logic';

describe('groupsQueryKey', () => {
  it('returns the teacher key for teacher source', () => {
    expect(groupsQueryKey('teacher')).toBe(TEACHER_GROUPS_QUERY_KEY);
  });

  it('returns the tenant key for tenant source', () => {
    expect(groupsQueryKey('tenant')).toBe(GROUPS_QUERY_KEY);
  });
});

describe('findSelectedGroup', () => {
  const groups: ClassGroup[] = [
    { id: '1', name: 'Alpha' },
    { id: '2', name: 'Beta' },
  ];

  it('returns the matching group when found', () => {
    expect(findSelectedGroup(groups, '2')).toEqual({ id: '2', name: 'Beta' });
  });

  it('returns undefined when no group matches', () => {
    expect(findSelectedGroup(groups, '99')).toBeUndefined();
  });

  it('returns undefined for an empty list', () => {
    expect(findSelectedGroup([], '1')).toBeUndefined();
  });
});

describe('resolveGroupCreate', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveGroupCreate(undefined)).toEqual({ kind: 'skip' });
  });

  it('creates with the provided name', () => {
    expect(resolveGroupCreate('Yoga')).toEqual({ kind: 'create', name: 'Yoga' });
  });
});

describe('resolveGroupRename', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveGroupRename('Yoga', undefined)).toEqual({ kind: 'skip' });
  });

  it('skips when the name is unchanged', () => {
    expect(resolveGroupRename('Yoga', 'Yoga')).toEqual({ kind: 'skip' });
  });

  it('renames when the name changed', () => {
    expect(resolveGroupRename('Yoga', 'Pilates')).toEqual({ kind: 'rename', name: 'Pilates' });
  });
});
