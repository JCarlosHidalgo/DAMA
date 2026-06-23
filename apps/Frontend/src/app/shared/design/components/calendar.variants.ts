import { tv } from 'tailwind-variants';

export const calendarStyles = tv({
  slots: {
    toolbar: 'flex flex-wrap items-center gap-3',
    nav: 'flex items-center gap-1',
    title: 'ml-2 font-semibold',
    spacer: 'flex-1',
    calendarHost: 'rounded-lg bg-[var(--mat-sys-surface)] p-2',
  },
});
