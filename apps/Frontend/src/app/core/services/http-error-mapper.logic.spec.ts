import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { ErrorMapOptions, extractServerMessage, mapHttpError } from './http-error-mapper.logic';

describe('mapHttpError', () => {
  describe('non-HttpErrorResponse input', () => {
    it('returns the default fallback for a plain Error', () => {
      expect(mapHttpError(new Error('boom'))).toBe('Ocurrió un error. Intenta nuevamente.');
    });

    it('returns the default fallback for a string', () => {
      expect(mapHttpError('oops')).toBe('Ocurrió un error. Intenta nuevamente.');
    });

    it('returns a custom fallback when provided', () => {
      const options: ErrorMapOptions = { fallback: 'Custom fallback' };
      expect(mapHttpError(new Error('x'), options)).toBe('Custom fallback');
    });
  });

  describe('byStatus override', () => {
    it('returns the overridden message when the status matches', () => {
      const options: ErrorMapOptions = { byStatus: { 400: 'Credenciales inválidas' } };
      const error = new HttpErrorResponse({ status: 400 });
      expect(mapHttpError(error, options)).toBe('Credenciales inválidas');
    });

    it('falls through to the default logic when the status does not match', () => {
      const options: ErrorMapOptions = { byStatus: { 400: 'Credenciales inválidas' } };
      const error = new HttpErrorResponse({ status: 401 });
      expect(mapHttpError(error, options)).toBe('Tu sesión expiró. Inicia sesión nuevamente.');
    });
  });

  describe('status 0', () => {
    it('returns the connection error message', () => {
      const error = new HttpErrorResponse({ status: 0 });
      expect(mapHttpError(error)).toBe('No se pudo conectar al servidor. Revisa tu conexión.');
    });
  });

  describe('status 400', () => {
    it('returns the server message when available', () => {
      const error = new HttpErrorResponse({ status: 400, error: 'Campo requerido' });
      expect(mapHttpError(error)).toBe('Campo requerido');
    });

    it('returns the default validation message when no server message', () => {
      const error = new HttpErrorResponse({ status: 400 });
      expect(mapHttpError(error)).toBe('Revisa los datos ingresados.');
    });
  });

  describe('status 422', () => {
    it('returns the server message when available', () => {
      const error = new HttpErrorResponse({ status: 422, error: { message: 'Datos inválidos' } });
      expect(mapHttpError(error)).toBe('Datos inválidos');
    });

    it('returns the default validation message when no server message', () => {
      const error = new HttpErrorResponse({ status: 422 });
      expect(mapHttpError(error)).toBe('Revisa los datos ingresados.');
    });
  });

  describe('status 401', () => {
    it('returns the session expired message', () => {
      const error = new HttpErrorResponse({ status: 401 });
      expect(mapHttpError(error)).toBe('Tu sesión expiró. Inicia sesión nuevamente.');
    });
  });

  describe('status 403', () => {
    it('returns the forbidden message', () => {
      const error = new HttpErrorResponse({ status: 403 });
      expect(mapHttpError(error)).toBe('No tienes permiso para realizar esta acción.');
    });
  });

  describe('status 404', () => {
    it('returns the server message when available', () => {
      const error = new HttpErrorResponse({
        status: 404,
        error: { detail: 'Recurso inexistente' },
      });
      expect(mapHttpError(error)).toBe('Recurso inexistente');
    });

    it('returns the default not-found message when no server message', () => {
      const error = new HttpErrorResponse({ status: 404 });
      expect(mapHttpError(error)).toBe('No se encontró el recurso solicitado.');
    });
  });

  describe('status 409', () => {
    it('returns the server message when available', () => {
      const error = new HttpErrorResponse({ status: 409, error: { title: 'Conflicto de datos' } });
      expect(mapHttpError(error)).toBe('Conflicto de datos');
    });

    it('returns the default conflict message when no server message', () => {
      const error = new HttpErrorResponse({ status: 409 });
      expect(mapHttpError(error)).toBe('La operación entra en conflicto con el estado actual.');
    });
  });

  describe('status >= 500', () => {
    it('returns the server error message for 500', () => {
      const error = new HttpErrorResponse({ status: 500 });
      expect(mapHttpError(error)).toBe('Error del servidor. Intenta nuevamente en unos momentos.');
    });

    it('returns the server error message for 503', () => {
      const error = new HttpErrorResponse({ status: 503 });
      expect(mapHttpError(error)).toBe('Error del servidor. Intenta nuevamente en unos momentos.');
    });
  });

  describe('other/default status (418)', () => {
    it('returns the server message when available', () => {
      const error = new HttpErrorResponse({ status: 418, error: 'I am a teapot' });
      expect(mapHttpError(error)).toBe('I am a teapot');
    });

    it('returns the fallback when no server message', () => {
      const error = new HttpErrorResponse({ status: 418 });
      expect(mapHttpError(error)).toBe('Ocurrió un error. Intenta nuevamente.');
    });

    it('returns a custom fallback when provided and no server message', () => {
      const options: ErrorMapOptions = { fallback: 'Custom default' };
      const error = new HttpErrorResponse({ status: 418 });
      expect(mapHttpError(error, options)).toBe('Custom default');
    });
  });
});

describe('extractServerMessage', () => {
  it('returns a non-empty string body directly', () => {
    const error = new HttpErrorResponse({ status: 400, error: 'Error message string' });
    expect(extractServerMessage(error)).toBe('Error message string');
  });

  it('returns null for an empty string body', () => {
    const error = new HttpErrorResponse({ status: 400, error: '' });
    expect(extractServerMessage(error)).toBeNull();
  });

  it('returns null for a whitespace-only string body', () => {
    const error = new HttpErrorResponse({ status: 400, error: '   ' });
    expect(extractServerMessage(error)).toBeNull();
  });

  it('returns null for a null body', () => {
    const error = new HttpErrorResponse({ status: 400, error: null });
    expect(extractServerMessage(error)).toBeNull();
  });

  it('returns null for an undefined body', () => {
    const error = new HttpErrorResponse({ status: 400 });
    expect(extractServerMessage(error)).toBeNull();
  });

  it('returns the message field from an object body', () => {
    const error = new HttpErrorResponse({ status: 400, error: { message: 'From message' } });
    expect(extractServerMessage(error)).toBe('From message');
  });

  it('returns the detail field when message is absent', () => {
    const error = new HttpErrorResponse({ status: 400, error: { detail: 'From detail' } });
    expect(extractServerMessage(error)).toBe('From detail');
  });

  it('returns the title field when message and detail are absent', () => {
    const error = new HttpErrorResponse({ status: 400, error: { title: 'From title' } });
    expect(extractServerMessage(error)).toBe('From title');
  });

  it('returns null when the object has none of message/detail/title', () => {
    const error = new HttpErrorResponse({ status: 400, error: { code: 42 } });
    expect(extractServerMessage(error)).toBeNull();
  });

  it('prefers message over detail when both are present', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { message: 'Primary', detail: 'Secondary' },
    });
    expect(extractServerMessage(error)).toBe('Primary');
  });

  it('skips empty string message and falls through to detail', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { message: '', detail: 'Fallthrough' },
    });
    expect(extractServerMessage(error)).toBe('Fallthrough');
  });
});
