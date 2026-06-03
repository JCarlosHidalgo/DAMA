import { tv } from '@shared/design';

export const loginStyles = tv({
  slots: {
    grid: 'grid min-h-dvh grid-cols-1 md:grid-cols-[1.1fr_1fr]',
    brandPanel:
      'flex flex-col justify-center border-b border-border px-6 py-8 bg-[linear-gradient(135deg,color-mix(in_oklab,var(--dama-primary-soft)_70%,var(--dama-bg)),var(--dama-bg)_80%)] md:border-b-0 md:border-r md:p-16',
    brand: 'mb-8 flex items-center gap-form',
    brandMark:
      'grid h-10 w-10 place-items-center rounded-control bg-primary text-[18px] font-bold tracking-[-0.02em] text-primary-fg shadow-[var(--dama-shadow-sm)]',
    brandName: 'text-[18px] font-bold tracking-[-0.02em] text-text-strong',
    title: 't-display',
    tagline: 't-body-md mt-2 max-w-[38ch] text-text-muted',
    cardPanel: 'flex items-center justify-center p-6 md:p-12',
    card: 'w-full max-w-[420px] p-card-sm shadow-pop!',
    form: 'mt-3 flex flex-col gap-1',
    error: 'm-0 mb-2 text-[12.5px] text-danger',
    submit: 'mt-2 h-12',
  },
});
