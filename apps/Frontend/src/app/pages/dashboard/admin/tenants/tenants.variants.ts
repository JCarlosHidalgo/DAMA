import { tv } from 'tailwind-variants';

export const tenantsStyles = tv({
  slots: {
    grid: 'grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-4',
    card: 'cursor-pointer transition-shadow duration-150 hover:shadow-[0_4px_16px_rgb(0_0_0/0.12)] focus-visible:shadow-[0_4px_16px_rgb(0_0_0/0.12)]',
    cardContent: 'flex flex-col gap-1',
    cardIcon: 'mb-2 text-[1.75rem] text-[var(--mat-sys-primary,#3f51b5)]',
    cardName: 'm-0 text-[1.1rem] font-semibold',
    cardTz: 'm-0 text-[0.85rem] text-[rgb(0_0_0/0.6)]',
  },
});
