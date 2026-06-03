import { tv } from 'tailwind-variants';

export const attendanceQrDialogStyles = tv({
  slots: {
    viewToggle: 'mx-6 mb-2 flex',
    viewToggleButton: 'flex-1',
    content: '',
    qrPane: 'flex flex-[0_0_auto] flex-col items-center',
    hint: 't-small mt-3 text-center text-text-muted',
    rosterPane: 'flex max-h-[340px] min-w-[240px] flex-col flex-[1_1_280px]',
    rosterHead:
      'flex items-center justify-between border-b border-[var(--mat-sys-outline-variant,rgba(0,0,0,0.12))] px-1 pt-1 pb-2',
    rosterCount: 'font-semibold',
    empty: 't-small py-4 text-center text-text-muted',
    rosterList: 'flex-1 overflow-y-auto p-0',
    badge:
      'animate-[badge-fade_4s_forwards] rounded-[10px] bg-[var(--mat-sys-primary-container,#d0e4ff)] px-2 py-0.5 text-[11px] font-semibold text-[var(--mat-sys-on-primary-container,#001b3d)]',
  },
  variants: {
    split: {
      true: { content: 'flex items-stretch gap-6' },
    },
  },
});
