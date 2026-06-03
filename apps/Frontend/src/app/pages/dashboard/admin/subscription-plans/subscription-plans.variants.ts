import { tv } from 'tailwind-variants';

export const adminSubscriptionPlansStyles = tv({
  slots: {
    grid: 'grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-4',
    form: 'flex flex-col gap-2',
    field: 'w-full',
  },
});
