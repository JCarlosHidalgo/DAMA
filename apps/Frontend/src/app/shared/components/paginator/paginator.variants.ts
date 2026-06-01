import { tv } from 'tailwind-variants';

export const paginatorStyles = tv({
  slots: {
    root: 'inline-flex items-center gap-1 p-1 border border-border rounded-[var(--dama-radius)] bg-surface',
    label: 'text-[13px] text-text-muted px-2 min-w-16 text-center tabular-nums',
  },
});
