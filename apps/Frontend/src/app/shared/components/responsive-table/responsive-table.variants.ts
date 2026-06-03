import { tv } from '@shared/design';

export const responsiveTableStyles = tv({
  slots: {
    tableWrap: 'overflow-x-auto',
    table: 'w-full',
    cardList: 'flex flex-col gap-grid p-card-sm',
    card: 'flex flex-col gap-snug rounded-card border border-border bg-surface p-card-sm shadow-card',
    cardRow: 'flex items-center justify-between gap-grid',
    cardLabel: 't-label-up shrink-0',
    cardValue: 'min-w-0 text-right',
    cardBlock: 'flex items-center justify-end gap-snug border-t border-divider pt-2',
  },
});
