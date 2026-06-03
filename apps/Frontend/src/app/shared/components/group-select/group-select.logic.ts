import { ClassGroup } from '@core/models';

export const GROUPS_QUERY_KEY = ['class-groups'] as const;
export const TEACHER_GROUPS_QUERY_KEY = ['teacher-class-groups'] as const;

export type GroupSource = 'tenant' | 'teacher';

export type GroupCreateOutcome = { kind: 'skip' } | { kind: 'create'; name: string };
export type GroupRenameOutcome = { kind: 'skip' } | { kind: 'rename'; name: string };

export function groupsQueryKey(source: GroupSource): readonly string[] {
  return source === 'teacher' ? TEACHER_GROUPS_QUERY_KEY : GROUPS_QUERY_KEY;
}

export function findSelectedGroup(
  groups: ClassGroup[],
  selectedGroupId: string,
): ClassGroup | undefined {
  return groups.find((group) => group.id === selectedGroupId);
}

export function resolveGroupCreate(dialogResult: string | undefined): GroupCreateOutcome {
  if (!dialogResult) {
    return { kind: 'skip' };
  }
  return { kind: 'create', name: dialogResult };
}

export function resolveGroupRename(
  currentName: string,
  dialogResult: string | undefined,
): GroupRenameOutcome {
  if (!dialogResult || dialogResult === currentName) {
    return { kind: 'skip' };
  }
  return { kind: 'rename', name: dialogResult };
}
