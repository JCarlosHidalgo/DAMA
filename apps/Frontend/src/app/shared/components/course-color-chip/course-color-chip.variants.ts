import { tv } from 'tailwind-variants';

export const courseColorChipStyles = tv({
  slots: {
    cchip: 'inline-flex items-center gap-2 font-medium text-text',
    dot: 'h-2.5 w-2.5 shrink-0 rounded-[3px]',
  },
});
