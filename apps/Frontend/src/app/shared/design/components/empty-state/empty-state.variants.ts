import { tv } from 'tailwind-variants';

export const emptyStateStyles = tv({
  slots: {
    root: 'py-9 px-5 text-center text-text-faint',
    icon: 'inline-block text-[28px] opacity-50 mb-2.5',
    message: 'text-text-muted mb-3',
  },
});
