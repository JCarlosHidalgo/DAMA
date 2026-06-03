import { tv } from 'tailwind-variants';

export const rechargeStyles = tv({
  slots: {
    grid: 'grid grid-cols-[repeat(auto-fit,minmax(320px,1fr))] gap-4',
    form: 'flex flex-col gap-3',
    field: 'w-full',
    submit: 'h-11 min-w-[140px] self-end',
    callout:
      'mb-3.5 flex items-start gap-2.5 rounded-[var(--dama-radius-sm)] border border-[color-mix(in_oklab,var(--dama-warning)_30%,transparent)] bg-warning-soft px-3.5 py-3 text-[13px] leading-[1.4] text-[color-mix(in_oklab,var(--dama-warning)_80%,var(--dama-text))]',
    calloutIcon: 'mt-px shrink-0 text-[16px] text-warning',
    calloutText: 'm-0',
  },
});
