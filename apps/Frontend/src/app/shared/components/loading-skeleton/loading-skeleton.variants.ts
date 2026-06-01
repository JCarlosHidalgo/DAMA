import { tv } from 'tailwind-variants';

export const loadingSkeletonStyles = tv({
  slots: {
    bar: 'rounded-[var(--dama-radius-sm)] bg-[linear-gradient(90deg,var(--dama-bg-2)_0%,var(--dama-surface-hover)_50%,var(--dama-bg-2)_100%)] bg-[length:200%_100%] animate-[dama-skel-shimmer_1.4s_ease-in-out_infinite] motion-reduce:animate-none',
  },
});
