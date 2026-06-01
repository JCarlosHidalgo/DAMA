import { tv } from 'tailwind-variants';

export const groupSelectStyles = tv({
  slots: {
    root: 'flex items-center gap-2 flex-wrap',
    field: 'min-w-[200px]',
    actions: 'flex items-center gap-1',
  },
});
