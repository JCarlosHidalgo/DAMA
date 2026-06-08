export interface SubscriptionRevenueTotal {
  totalRevenue: number;
  paymentCount: number;
  currency: string;
}

export interface SubscriptionRevenuePoint {
  year: number;
  month: number;
  amount: number;
  count: number;
}

export interface SubscriptionRevenueByTier {
  level: number;
  revenue: number;
  count: number;
}

export interface TenantTierCount {
  tier: number;
  tenantCount: number;
}
