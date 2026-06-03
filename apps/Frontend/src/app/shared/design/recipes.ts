import { createTV } from 'tailwind-variants';

export const tv = createTV({
  twMergeConfig: {
    extend: {
      theme: {
        spacing: ['snug', 'form', 'grid', 'card', 'card-sm', 'dialog', 'dialog-sm'],
        radius: ['card', 'control', 'callout'],
        shadow: ['card', 'pop'],
      },
    },
  },
});

export type Tone = 'neutral' | 'primary' | 'success' | 'warning' | 'danger';

const toneClasses: Record<Tone, string> = {
  neutral: 'border-border bg-bg-2 text-text-muted',
  primary: 'border-[var(--dama-primary-border)] bg-primary-soft text-primary',
  success:
    'border-[color-mix(in_oklab,var(--dama-success)_30%,transparent)] bg-success-soft text-success',
  warning:
    'border-[color-mix(in_oklab,var(--dama-warning)_30%,transparent)] bg-warning-soft text-warning',
  danger:
    'border-[color-mix(in_oklab,var(--dama-danger)_30%,transparent)] bg-danger-soft text-danger',
};

export const surfaceCard = tv({
  slots: {
    root: 'rounded-card border border-border bg-surface shadow-card',
  },
  variants: {
    pad: {
      md: { root: 'p-card' },
      sm: { root: 'p-card-sm' },
      none: { root: 'p-0' },
    },
  },
  defaultVariants: {
    pad: 'md',
  },
});

export const formStack = tv({
  slots: {
    root: 'flex flex-col gap-form',
  },
  variants: {
    width: {
      auto: {},
      dialog: { root: 'min-w-dialog' },
      dialogSm: { root: 'min-w-dialog-sm' },
    },
  },
  defaultVariants: {
    width: 'auto',
  },
});

export const pillBadge = tv({
  slots: {
    root: 'inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-semibold',
    dot: 'h-1.5 w-1.5 rounded-full bg-[currentColor]',
  },
  variants: {
    variant: {
      neutral: { root: toneClasses.neutral },
      primary: { root: toneClasses.primary },
      success: { root: toneClasses.success },
      warning: { root: toneClasses.warning },
      danger: { root: toneClasses.danger },
    },
  },
  defaultVariants: {
    variant: 'neutral',
  },
});

export const calloutBox = tv({
  slots: {
    root: 'flex items-start gap-snug rounded-callout border px-3.5 py-3',
  },
  variants: {
    variant: {
      neutral: { root: toneClasses.neutral },
      primary: { root: toneClasses.primary },
      success: { root: toneClasses.success },
      warning: { root: toneClasses.warning },
      danger: { root: toneClasses.danger },
    },
  },
  defaultVariants: {
    variant: 'warning',
  },
});
