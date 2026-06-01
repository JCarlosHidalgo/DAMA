import { tv } from 'tailwind-variants';

export const qrCardStyles = tv({
  slots: {
    root: 'inline-flex flex-col items-center gap-3 rounded-[var(--dama-radius-md)] border border-border bg-[#ffffff] p-5',
    wrap: 'flex items-center justify-center',
    meta: 'text-center text-[#0f172a]',
    title: 't-h2',
    subtitle: 't-small text-[rgba(15,23,42,0.6)]',
  },
});
