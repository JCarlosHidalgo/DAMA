import { tv } from 'tailwind-variants';

export const markAttendanceStatusStyles = tv({
  base: 'mt-3.5 flex flex-wrap items-center justify-center gap-2.5 rounded-[var(--dama-radius-sm)] p-4',
  variants: {
    tone: {
      neutral: '',
      ok: 'bg-success-soft text-success',
      err: 'bg-danger-soft text-danger',
    },
  },
});

export const markAttendanceStyles = tv({
  slots: {
    scannerCard: 'mx-auto max-w-[600px]',
    zeroState:
      'flex flex-col items-center gap-1.5 rounded-[var(--dama-radius-md)] border border-[color-mix(in_oklab,var(--dama-danger)_25%,transparent)] bg-danger-soft px-5 py-9 text-center',
    zeroIcon: 'mb-2 text-[48px] text-danger',
    zeroTitle: 't-h2 m-0 text-text',
    zeroHint: 't-small text-text-muted',
  },
});
