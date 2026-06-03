import { tv } from 'tailwind-variants';

export const scheduleDialogStyles = tv({
  slots: {
    form: 'flex flex-col gap-3 min-w-[360px]',
    field: 'w-full',
  },
});

export const scheduleClassTagStyles = tv({
  base: 'ml-1.5 inline-block rounded-full px-2 py-px align-middle text-[11px] font-semibold',
  variants: {
    kind: {
      weekly:
        'bg-[var(--mat-sys-secondary-container)] text-[var(--mat-sys-on-secondary-container)]',
      unique: 'bg-[var(--mat-sys-tertiary-container)] text-[var(--mat-sys-on-tertiary-container)]',
    },
  },
});

export const scheduleStyles = tv({
  slots: {
    controlsCard: 'mb-3',
    controls: 'flex flex-wrap items-center gap-3',
    columns: 'mb-3 grid grid-cols-1 gap-3',
    colCard: 'p-0',
    colHead: 'mx-1 mt-1 mb-3 flex flex-wrap items-center justify-between gap-3',
    colTitle: 'm-0 font-semibold',
    colHeadActions: 'flex flex-wrap items-center gap-3',
    daySelect: 'w-[140px]',
    groupSelectField: 'min-w-[160px] flex-1',
    classList: 'flex min-h-[60px] flex-col gap-2',
    classItem:
      'flex cursor-grab items-center justify-between gap-2 rounded-lg border border-[var(--mat-sys-outline-variant,rgba(0,0,0,0.12))] bg-[var(--mat-sys-surface)] px-3 py-2.5',
    classMain: 'flex flex-col gap-0.5',
    classCourse: 'font-semibold',
    classTime: 'inline-flex items-center gap-1.5 tabular-nums',
    classTeachers: 't-small text-text-muted',
    empty: 't-small py-4 text-center text-text-muted',
    dangerButton: 'text-danger',
    calCard: 'p-0',
    calCardContent: 'p-3',
    buttonLabel: 'ml-1.5',
  },
  variants: {
    split: {
      true: { columns: 'grid-cols-2 max-[768px]:grid-cols-1' },
    },
  },
});
