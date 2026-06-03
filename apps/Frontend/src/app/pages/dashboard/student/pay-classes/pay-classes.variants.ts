import { tv } from 'tailwind-variants';

export const qrImageDialogStyles = tv({
  slots: {
    content: 'flex min-w-[280px] flex-col items-center gap-3',
    image: 'block h-auto w-full max-w-[320px]',
    hint: 't-small m-0 text-center text-text-muted',
  },
});

export const noPaymentCredentialsDialogStyles = tv({
  slots: {
    message: 't-body m-0 min-w-[280px] text-text',
  },
});

export const payDialogStyles = tv({
  slots: {
    info: 'mb-3',
    form: 'flex flex-col gap-3 min-w-[320px]',
    methods: 'flex flex-col gap-2',
    field: 'w-full',
  },
});

export const payClassesStyles = tv({
  slots: {
    banner:
      'mb-4 flex items-center gap-2.5 rounded-[var(--dama-radius)] border border-[color-mix(in_oklab,var(--dama-warning)_40%,transparent)] bg-[color-mix(in_oklab,var(--dama-warning)_14%,transparent)] px-4 py-3 text-text-strong',
    grid: 'grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-4',
    packCard: 'flex flex-col',
    packHead: 'mb-3 flex items-center justify-between gap-2',
    price: 't-num-lg tabular-nums text-text-strong',
    buttonLabel: 'ml-1.5',
  },
});
