import { tv } from '@shared/design';

export const courseDialogStyles = tv({
  slots: {
    field: 'w-full min-w-dialog',
  },
});

export const coursesStyles = tv({
  slots: {
    listCard: 'p-0',
    cardContent: 'p-0',
    skelStack: 'flex flex-col gap-3 p-5',
    dangerButton: 'text-danger',
    buttonLabel: 'ml-1.5',
  },
});
