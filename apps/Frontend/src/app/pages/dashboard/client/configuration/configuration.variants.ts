import { tv } from 'tailwind-variants';

export const clientConfigurationStyles = tv({
  slots: {
    card: 'mb-5 rounded-[var(--dama-radius-md)] border border-border bg-surface px-5 py-4 shadow-[var(--dama-shadow-xs)]',
    hint: 't-body-sm mt-1 mb-4 text-text-muted',
    field: 'min-w-[320px]',
    statusRow: 'mb-3 flex items-center gap-4',
    keyValue: 't-body-md tabular-nums',
    errorText: 't-body-sm text-[var(--dama-error,#b3261e)]',
    editForm: 'mt-4 flex flex-wrap items-start gap-4',
  },
});
