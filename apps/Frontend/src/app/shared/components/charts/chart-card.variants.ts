import { surfaceCard, tv } from '@shared/design';

export const chartCardStyles = tv({
  extend: surfaceCard,
  slots: {
    root: 'flex flex-col gap-snug',
    title: 't-label-up text-text-muted',
    canvasWrap: 'relative h-[280px] w-full',
  },
});
