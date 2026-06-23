import { tv } from 'tailwind-variants';

export const pageHeadStyles = tv({
  slots: {
    root: 'flex items-start justify-between gap-4 mb-5 max-sm:flex-col max-sm:items-stretch',
    text: '',
    title: 't-h1 m-0',
    subtitle: 't-small mt-[2px] mb-0',
    actions: 'flex gap-2 flex-wrap max-sm:w-full',
  },
});
