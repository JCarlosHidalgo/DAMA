import { tv } from 'tailwind-variants';

export const errorStateStyles = tv({
  slots: {
    root: 'px-5 py-9 text-center text-danger',
    icon: 'mb-2.5 inline-block text-[28px] opacity-85',
    message: 'm-0 mb-3 text-body font-normal leading-[1.5] tracking-[-0.005em] text-text',
  },
});
