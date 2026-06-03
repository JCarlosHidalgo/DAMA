import { tv } from 'tailwind-variants';

export const qrCardStyles = tv({
  slots: {
    root: 'inline-flex flex-col items-center gap-3 rounded-[var(--dama-radius-md)] border border-border bg-surface p-5',
    wrap: 'flex items-center justify-center rounded-[6px] bg-white p-2',
    meta: 'text-center text-text-strong',
    title: 't-h2',
    subtitle: 't-small text-text-muted',
  },
});
