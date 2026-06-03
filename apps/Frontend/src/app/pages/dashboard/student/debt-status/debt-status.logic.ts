export type TabKind = 'pending' | 'success' | 'failed';

const TAB_KINDS = ['pending', 'success', 'failed'] as const;

export function tabKindForIndex(index: number): TabKind {
  return TAB_KINDS[index] ?? 'pending';
}
