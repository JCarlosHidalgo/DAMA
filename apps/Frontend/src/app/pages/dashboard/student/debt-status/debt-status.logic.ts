import type { FailureReason, StudentQrBreakdown } from '@core/models';
import type { TagVariant } from '@shared/design/components';
import type { ChartPaletteKey } from '@shared/design/components/charts';

export type TabKind = 'pending' | 'success' | 'failed';

const TAB_KINDS = ['pending', 'success', 'failed'] as const;

export function tabKindForIndex(index: number): TabKind {
  return TAB_KINDS[index] ?? 'pending';
}

export interface BreakdownSeries {
  labels: string[];
  values: number[];
  colorKeys: ChartPaletteKey[];
}

export function breakdownToStatusDoughnut(breakdown: StudentQrBreakdown): BreakdownSeries {
  return {
    labels: ['Pendientes', 'Pagadas', 'Vencidas', 'Otras fallidas'],
    values: [
      breakdown.pendingCount,
      breakdown.successCount,
      breakdown.expiredCount,
      breakdown.otherFailedCount,
    ],
    colorKeys: ['primary', 'success', 'warning', 'danger'],
  };
}

export function breakdownToAmountsBar(breakdown: StudentQrBreakdown): BreakdownSeries {
  return {
    labels: ['Pendiente', 'Pagado', 'Vencido', 'Otras fallidas'],
    values: [
      breakdown.pendingAmount,
      breakdown.successAmount,
      breakdown.expiredAmount,
      breakdown.otherFailedAmount,
    ],
    colorKeys: ['primary', 'success', 'warning', 'danger'],
  };
}

export function failureReasonTone(reason: FailureReason): TagVariant {
  switch (reason) {
    case 'Expired':
      return 'warning';
    case 'CallbackError':
      return 'danger';
    case 'Manual':
      return 'neutral';
  }
}

export function failureReasonLabel(reason: FailureReason): string {
  switch (reason) {
    case 'Expired':
      return 'Vencida';
    case 'CallbackError':
      return 'Fallida';
    case 'Manual':
      return 'Manual';
  }
}
