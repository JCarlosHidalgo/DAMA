import { tv } from 'tailwind-variants';

export const dashboardStyles = tv({
  slots: {
    container: 'h-dvh bg-bg',
    brand:
      'mb-2 flex items-center gap-3 border-b border-divider px-4 py-[18px] text-[16px] font-bold tracking-[-0.01em] text-text-strong',
    brandIcon:
      'grid h-8 w-8 place-items-center rounded-[var(--dama-radius-sm)] bg-primary text-[14px] text-primary-fg shadow-[var(--dama-shadow-xs)]',
    brandText: '',
    navLabel: '',
    toolbarSpacer: 'flex-auto',
    userLabel: 'mr-2 text-[13px] font-medium text-text-muted max-sm:hidden',
    content:
      'mx-auto min-h-[calc(100dvh-var(--dama-toolbar-h))] max-w-[1280px] bg-bg p-[clamp(16px,3vw,24px)] max-sm:p-4',
  },
  variants: {
    collapsed: {
      true: { brandText: 'hidden', navLabel: 'hidden' },
    },
  },
});
