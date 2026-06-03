import { describe, it, expect } from 'vitest';

import { resolveTenantCreate, resolveTenantEdit } from './tenants.logic';

describe('resolveTenantCreate', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveTenantCreate(undefined)).toEqual({ kind: 'skip' });
  });

  it('creates with the provided name', () => {
    expect(resolveTenantCreate('Escuela')).toEqual({ kind: 'create', name: 'Escuela' });
  });
});

describe('resolveTenantEdit', () => {
  it('skips when the dialog was dismissed', () => {
    expect(resolveTenantEdit('Escuela', undefined)).toEqual({ kind: 'skip' });
  });

  it('skips when the name is unchanged', () => {
    expect(resolveTenantEdit('Escuela', 'Escuela')).toEqual({ kind: 'skip' });
  });

  it('updates when the name changed', () => {
    expect(resolveTenantEdit('Escuela', 'Colegio')).toEqual({ kind: 'update', name: 'Colegio' });
  });
});
