import { tv } from 'tailwind-variants';

export const adminAnalyticsStyles = tv({
  slots: {
    kpiGrid: 'mb-grid grid grid-cols-1 gap-grid sm:grid-cols-2',
    chartsGrid: 'grid grid-cols-1 gap-grid lg:grid-cols-2',
    timelineWide: 'lg:col-span-2',
  },
});
