import { tv } from 'tailwind-variants';

export const confirmationDialogStyles = tv({
  slots: {
    message: 't-body m-0 text-text',
    confirmButton: '',
  },
  variants: {
    destructive: {
      true: {
        confirmButton:
          '[--mdc-filled-button-container-color:var(--dama-danger)] [--mdc-filled-button-label-text-color:var(--dama-primary-fg)] hover:[--mdc-filled-button-container-color:color-mix(in_oklab,var(--dama-danger)_85%,black)]',
      },
    },
  },
});
