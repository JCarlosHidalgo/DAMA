export interface PaymentSummary {
  totalEarnings: number;
  monthEarnings: number;
  currency: string;
  firstPaymentDate: string | null;
  from: string;
  to: string;
}

export interface DebtTemplate {
  id: string;
  description: string;
  classQuantity: number;
  cost: number;
  currency: string;
  tenantId: string;
}

export interface CreateDebtTemplatePayload {
  description: string;
  classQuantity: number;
  cost: number;
  externalReference?: string | null;
}

export interface UpdateDebtTemplatePayload {
  description: string;
  classQuantity: number;
  cost: number;
}

export interface PendingQrPayment {
  id: string;
  tenantId: string;
  studentId: string;
  classQuantity: number;
  cost: number;
  currency: string;
  externalReference: string;
  qrImageUrl: string;
  createdAt: string;
}

export interface SuccessQrPayment {
  id: string;
  tenantId: string;
  studentId: string;
  classQuantity: number;
  cost: number;
  currency: string;
  paidAt: string;
}

export type FailureReason = 'Expired' | 'CallbackError' | 'Manual';

export interface FailedQrPayment {
  id: string;
  tenantId: string;
  studentId: string;
  classQuantity: number;
  cost: number;
  currency: string;
  failedAt: string;
  failureReason: FailureReason;
}

export interface StudentQrBreakdown {
  pendingCount: number;
  pendingAmount: number;
  successCount: number;
  successAmount: number;
  expiredCount: number;
  expiredAmount: number;
  otherFailedCount: number;
  otherFailedAmount: number;
  currency: string;
}

export interface StudentSpendPoint {
  year: number;
  month: number;
  amount: number;
  count: number;
}

export interface QrDebtPending {
  identificadorDeuda: string;
  status: 'Pending';
  alreadyGenerated: boolean;
}

export type QrDebtStatusValue = 'Pending' | 'Ready' | 'Failed';

export interface QrDebtStatus {
  identificadorDeuda: string;
  status: QrDebtStatusValue;
  qrSimpleUrl?: string | null;
  error?: string | null;
}

export interface TodotixAppKeyStatus {
  hasCustomKey: boolean;
  maskedAppKey: string | null;
}

export interface TodotixAppKeyReveal {
  appKey: string;
}

export interface UpdateTodotixAppKeyPayload {
  appKey: string;
}

export interface PaymentAvailability {
  hasPaymentCredentials: boolean;
}

export type SubscriptionDurationUnit = 'Day' | 'Week' | 'Month';

export interface SubscriptionPlan {
  level: number;
  price: number;
  currency: string;
  durationAmount: number;
  durationUnit: SubscriptionDurationUnit;
}

export interface UpdateSubscriptionPlanPayload {
  price: number;
  durationAmount: number;
  durationUnit: SubscriptionDurationUnit;
}
