import { tv } from 'tailwind-variants';

export const cameraScannerStyles = tv({
  slots: {
    wrap: 'relative mx-auto aspect-square max-w-[480px] overflow-hidden rounded-[var(--dama-radius-md)] bg-black',
    bracket: 'pointer-events-none absolute h-7 w-7 border-[3px] border-white/90',
    bracketTopLeft: 'top-3 left-3 rounded-tl-[6px] border-r-0 border-b-0',
    bracketTopRight: 'top-3 right-3 rounded-tr-[6px] border-b-0 border-l-0',
    bracketBottomLeft: 'bottom-3 left-3 rounded-bl-[6px] border-t-0 border-r-0',
    bracketBottomRight: 'right-3 bottom-3 rounded-br-[6px] border-t-0 border-l-0',
  },
});
