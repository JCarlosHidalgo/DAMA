import { tv } from 'tailwind-variants';

export const statCardStyles = tv({
  slots: {
    root: 'flex min-h-[120px] flex-col gap-2 rounded-[var(--dama-radius-md)] border border-border bg-surface p-5 shadow-[var(--dama-shadow-xs)]',
    head: 'flex items-center justify-between text-text-faint [&_app-icon]:text-[14px] [&_app-icon]:opacity-60',
    label: 't-label-up',
    value: 't-num-lg tabular-nums m-0',
    delta:
      'self-start rounded-full px-2 py-[2px] text-[12px] font-semibold bg-bg-2 text-text-muted',
    sub: 't-small text-text-muted',
  },
  variants: {
    sign: {
      up: { delta: 'bg-success-soft text-success' },
      down: { delta: 'bg-danger-soft text-danger' },
    },
  },
});
