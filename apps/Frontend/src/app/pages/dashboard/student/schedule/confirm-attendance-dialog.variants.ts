import { tv } from 'tailwind-variants';

export const confirmAttendanceDialogStyles = tv({
  slots: {
    detail: 'flex min-w-[280px] flex-col gap-1.5 py-2',
    course: 'text-[1.1rem] font-semibold',
    time: 'inline-flex items-center gap-1.5 tabular-nums',
    date: 't-small text-text-muted',
  },
});
