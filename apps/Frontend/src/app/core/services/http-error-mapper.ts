import { Injectable } from '@angular/core';

import { type ErrorMapOptions, mapHttpError } from './http-error-mapper.logic';

export type { ErrorMapOptions } from './http-error-mapper.logic';

@Injectable({ providedIn: 'root' })
export class HttpErrorMapper {
  mapError(error: unknown, options: ErrorMapOptions = {}): string {
    return mapHttpError(error, options);
  }
}
