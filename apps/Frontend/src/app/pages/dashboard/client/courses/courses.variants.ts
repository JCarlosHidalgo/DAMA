import { tv } from 'tailwind-variants';

export const courseDialogStyles = tv({
  slots: {
    field: 'w-full min-w-[320px]',
  },
});

export const coursesStyles = tv({
  slots: {
    listCard: 'p-0',
    cardContent: 'p-0',
    skelStack: 'flex flex-col gap-3 p-5',
    tableWrap: 'overflow-x-auto',
    table: 'w-full',
    dangerButton: 'text-danger',
    buttonLabel: 'ml-1.5',
  },
});
