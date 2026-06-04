export function translateAttendanceError(error: unknown): string {
  const message = error instanceof Error ? error.message : '';
  if (message.includes('AlreadyMarked')) {
    return 'Ya registraste tu asistencia a esta clase.';
  }
  if (message.includes('NoRemainingClasses')) {
    return 'No tienes clases disponibles. Compra un paquete primero.';
  }
  if (message.includes('ClassFull')) {
    return 'La clase ya alcanzó su cupo máximo.';
  }
  if (message.includes('OutsideAllowedWindow')) {
    return 'Fuera del horario permitido (01:00–23:00 local).';
  }
  return 'No se pudo registrar la asistencia.';
}
