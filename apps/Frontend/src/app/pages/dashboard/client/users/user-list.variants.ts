import { tv } from '@shared/design';

export const userDialogStyles = tv({
  slots: {
    form: 'flex flex-col gap-form min-w-dialog',
    field: 'w-full',
  },
});

export const userListStyles = tv({
  slots: {
    listCard: 'p-0',
    cardContent: 'p-0',
    skelStack: 'flex flex-col gap-3 p-5',
    dangerButton: 'text-danger disabled:text-text-faint',
    userCell: 'inline-flex items-center gap-3 max-w-full',
    avatar:
      'inline-grid h-8 w-8 shrink-0 place-items-center rounded-full text-[12px] font-semibold tracking-[0.02em] text-white',
    paginatorWrap: 'flex justify-center border-t border-divider p-4',
    buttonLabel: 'ml-1.5',
  },
});
