import { Directive } from '@angular/core';

@Directive({
  selector: 'input[matInput]:not([type=password])',
  host: {
    'data-bwignore': 'true',
    'data-1p-ignore': 'true',
    'data-lpignore': 'true',
  },
})
export class NoPasswordManager {}
