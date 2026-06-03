import { tv } from 'tailwind-variants';

export const subscriptionQrImageDialogStyles = tv({
  slots: {
    content: 'flex min-w-[280px] flex-col items-center gap-3',
    image: 'block h-auto w-full max-w-[320px]',
    hint: 't-small m-0 text-center text-text-muted',
  },
});

export const subscriptionPayDialogStyles = tv({
  slots: {
    form: 'flex flex-col gap-3 min-w-[320px]',
    field: 'w-full',
  },
});

export const clientSubscriptionStyles = tv({
  slots: {
    statusCard: 'mb-4',
    statusHead: 'mb-3 flex flex-wrap items-center gap-3',
    grid: 'grid grid-cols-[repeat(auto-fit,minmax(260px,1fr))] gap-4',
    planHead: 'mb-2 flex items-center justify-between gap-2',
    planDesc: 't-small mb-3 text-text-muted',
    price: 't-num-lg tabular-nums text-text-strong',
  },
});
