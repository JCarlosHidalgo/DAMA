export function studentRechargeConfirmMessage(quantity: number, studentName: string): string {
  return `Agregar ${quantity} clase(s) a ${studentName}?`;
}

export function tenantRechargeConfirmMessage(quantity: number): string {
  return `Agregar ${quantity} clase(s) a TODOS los estudiantes con saldo previo. ¿Continuar?`;
}

export function studentRechargeSuccessMessage(quantity: number, studentName: string): string {
  return `Recargadas ${quantity} clase(s) a ${studentName}.`;
}

export function tenantRechargeSuccessMessage(affected: number): string {
  return `Actualizados ${affected} estudiantes.`;
}
