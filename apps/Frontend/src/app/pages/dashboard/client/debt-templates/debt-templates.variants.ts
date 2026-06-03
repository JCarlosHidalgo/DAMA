import { tv } from '@shared/design';

export const debtTemplateDialogStyles = tv({
  slots: {
    form: 'flex flex-col gap-form min-w-dialog',
    field: 'w-full',
  },
});

export const debtTemplatesStyles = tv({
  slots: {
    listCard: 'p-0',
    cardContent: 'p-0',
    skelStack: 'flex flex-col gap-3 p-5',
    tableWrap: 'overflow-x-auto',
    table: 'w-full',
    num: 'text-right',
    numMono: 'text-right tabular-nums',
    dangerButton: 'text-danger',
    buttonLabel: 'ml-1.5',
  },
});
