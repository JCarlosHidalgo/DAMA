import { surfaceCard, tv } from '@shared/design';

export const statCardStyles = tv({
  extend: surfaceCard,
  slots: {
    root: 'flex min-h-[120px] flex-col gap-snug',
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
