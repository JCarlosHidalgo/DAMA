import { tv } from 'tailwind-variants';

export const studentSummaryStyles = tv({
  slots: {
    remain: 'relative mx-auto max-w-[480px]',
    head: 'mb-2 flex items-baseline justify-between gap-3',
    count: 'mt-3 mb-4 flex items-center gap-4',
    countIcon: 'text-[40px] text-text-muted',
  },
  variants: {
    zero: {
      true: {
        remain: 'border-l-4 border-danger',
        countIcon: 'text-danger',
      },
    },
  },
});
