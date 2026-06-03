import { tv } from 'tailwind-variants';

export const clientSummaryStyles = tv({
  slots: {
    kpiGrid: 'grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-4',
    rangeCard:
      'col-span-full flex flex-col gap-1.5 rounded-[var(--dama-radius-md)] border border-border bg-surface px-5 py-4 shadow-[var(--dama-shadow-xs)]',
    rangeValue: 't-body-md text-text tabular-nums',
  },
});
