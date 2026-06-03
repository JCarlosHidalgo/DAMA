export type TenantCreateOutcome = { kind: 'skip' } | { kind: 'create'; name: string };
export type TenantEditOutcome = { kind: 'skip' } | { kind: 'update'; name: string };

export function resolveTenantCreate(dialogResult: string | undefined): TenantCreateOutcome {
  if (!dialogResult) {
    return { kind: 'skip' };
  }
  return { kind: 'create', name: dialogResult };
}

export function resolveTenantEdit(
  currentName: string,
  dialogResult: string | undefined,
): TenantEditOutcome {
  if (!dialogResult || dialogResult === currentName) {
    return { kind: 'skip' };
  }
  return { kind: 'update', name: dialogResult };
}
