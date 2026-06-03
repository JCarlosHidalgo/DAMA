export type TabKind = 'scheduled' | 'unique';

export function tabKindForIndex(index: number): TabKind {
  return index === 0 ? 'scheduled' : 'unique';
}

export function formatTimeRange(start: string, end: string): string {
  return `${start.slice(0, 5)} – ${end.slice(0, 5)}`;
}
