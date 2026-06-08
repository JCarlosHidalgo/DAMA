import { tv } from 'tailwind-variants';

export const debtStatusStyles = tv({
  slots: {
    kpiGrid: 'mb-grid grid grid-cols-2 gap-grid lg:grid-cols-4',
    chartsGrid: 'mb-grid grid grid-cols-1 gap-grid lg:grid-cols-2',
    tabsCard: 'p-0',
    cardContent: 'p-0',
    skelStack: 'flex flex-col gap-3 p-5',
    tableWrap: 'overflow-x-auto',
    table: 'w-full',
    num: 'text-right',
    numMono: 'text-right tabular-nums',
    paginatorWrap: 'flex justify-center border-t border-divider p-4',
  },
});
