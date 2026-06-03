export type CourseCreateOutcome = { kind: 'skip' } | { kind: 'create'; name: string };
export type CourseEditOutcome = { kind: 'skip' } | { kind: 'update'; name: string };

export function resolveCourseCreate(dialogResult: string | undefined): CourseCreateOutcome {
  if (!dialogResult) {
    return { kind: 'skip' };
  }
  return { kind: 'create', name: dialogResult };
}

export function resolveCourseEdit(
  currentName: string,
  dialogResult: string | undefined,
): CourseEditOutcome {
  if (!dialogResult || dialogResult === currentName) {
    return { kind: 'skip' };
  }
  return { kind: 'update', name: dialogResult };
}

export function coursesSubtitle(count: number): string {
  return `${count} curso(s)`;
}
