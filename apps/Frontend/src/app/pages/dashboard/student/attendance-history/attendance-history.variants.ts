import { tv } from 'tailwind-variants';

export const attendanceHistoryStyles = tv({
  slots: {
    tabsCard: 'p-0',
    cardContent: 'p-0',
    skelStack: 'flex flex-col gap-3 p-5',
    tableWrap: 'overflow-x-auto',
    table: 'w-full',
    paginatorWrap: 'flex justify-center border-t border-divider p-4',
  },
});
