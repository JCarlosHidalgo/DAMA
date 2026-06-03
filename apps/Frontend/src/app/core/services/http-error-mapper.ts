import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface ErrorMapOptions {
  fallback?: string;
  byStatus?: Record<number, string>;
}

const DEFAULT_FALLBACK = 'Ocurrió un error. Intenta nuevamente.';

@Injectable({ providedIn: 'root' })
export class HttpErrorMapper {
  mapError(error: unknown, options: ErrorMapOptions = {}): string {
    const fallback = options.fallback ?? DEFAULT_FALLBACK;
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }

    const override = options.byStatus?.[error.status];
    if (override) {
      return override;
    }

    const serverMessage = this.extractServerMessage(error);
    if (error.status === 0) {
      return 'No se pudo conectar al servidor. Revisa tu conexión.';
    }
    if (error.status === 400 || error.status === 422) {
      return serverMessage ?? 'Revisa los datos ingresados.';
    }
    if (error.status === 401) {
      return 'Tu sesión expiró. Inicia sesión nuevamente.';
    }
    if (error.status === 403) {
      return 'No tienes permiso para realizar esta acción.';
    }
    if (error.status === 404) {
      return serverMessage ?? 'No se encontró el recurso solicitado.';
    }
    if (error.status === 409) {
      return serverMessage ?? 'La operación entra en conflicto con el estado actual.';
    }
    if (error.status >= 500) {
      return 'Error del servidor. Intenta nuevamente en unos momentos.';
    }
    return serverMessage ?? fallback;
  }

  private extractServerMessage(error: HttpErrorResponse): string | null {
    const body: unknown = error.error;
    if (typeof body === 'string' && body.trim().length > 0) {
      return body;
    }
    if (body && typeof body === 'object') {
      const candidate = body as { message?: unknown; detail?: unknown; title?: unknown };
      for (const value of [candidate.message, candidate.detail, candidate.title]) {
        if (typeof value === 'string' && value.trim().length > 0) {
          return value;
        }
      }
    }
    return null;
  }
}
