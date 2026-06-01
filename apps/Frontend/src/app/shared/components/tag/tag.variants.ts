import { tv } from 'tailwind-variants';

export const tagStyles = tv({
  slots: {
    root: 'inline-flex items-center gap-1.5 rounded-full border border-border bg-bg-2 px-2 py-0.5 text-[11px] font-semibold text-text-muted',
    dot: 'h-1.5 w-1.5 rounded-full bg-[currentColor]',
  },
  variants: {
    variant: {
      neutral: {},
      primary: {
        root: 'border-[var(--dama-primary-border)] bg-primary-soft text-primary',
      },
      success: {
        root: 'border-[color-mix(in_oklab,var(--dama-success)_30%,transparent)] bg-[var(--dama-success-soft)] text-success',
      },
      warning: {
        root: 'border-[color-mix(in_oklab,var(--dama-warning)_30%,transparent)] bg-[var(--dama-warning-soft)] text-warning',
      },
      danger: {
        root: 'border-[color-mix(in_oklab,var(--dama-danger)_30%,transparent)] bg-[var(--dama-danger-soft)] text-danger',
      },
    },
  },
  defaultVariants: {
    variant: 'neutral',
  },
});
