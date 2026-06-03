import { tv } from 'tailwind-variants';

export const attendanceMarkedDialogStyles = tv({
  slots: {
    marked: 'flex flex-col items-center gap-3 px-4 pt-6 pb-2 text-center min-w-[260px]',
    icon: 'text-[64px] text-success',
    message: 't-h2 m-0 text-text',
  },
});
