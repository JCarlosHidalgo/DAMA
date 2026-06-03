import { tv } from 'tailwind-variants';

export const tenantsStyles = tv({
  slots: {
    grid: 'grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-4',
    card: 'cursor-pointer transition-shadow duration-150 hover:shadow-pop focus-visible:shadow-pop',
    cardContent: 'flex flex-col gap-1',
    cardIcon: 'mb-2 text-[1.75rem] text-primary',
    cardName: 'm-0 text-[1.1rem] font-semibold',
    cardTz: 'm-0 text-[0.85rem] text-text-muted',
  },
});
