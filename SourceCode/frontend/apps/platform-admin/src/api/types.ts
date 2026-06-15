export interface LoginRequest {
  username: string;
  password: string;
  tenantCode?: string | null;
}

export interface UserInfo {
  id: number;
  username: string;
  realName?: string | null;
  role: string;
  tenantId?: number | null;
  storeId?: number | null;
  isBlind: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserInfo;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface Plan {
  id: number;
  code: string;
  name: string;
  maxStores: number;
  maxStaff: number;
  annualPrice: number;
  featureJson?: string | null;
  isActive: boolean;
}

export interface TenantSummary {
  id: number;
  name: string;
  contactPhone: string;
  contactName?: string | null;
  status: 'Active' | 'Expired' | 'Disabled' | 'Trial';
  expireAt?: string | null;
  currentPlanId?: number | null;
  currentPlanName?: string | null;
  daysToExpire?: number | null;
  subscriptionStartAt?: string | null;
  subscriptionYears?: number | null;
}

export interface TenantDetail extends TenantSummary {
  storeCount: number;
  userCount: number;
  createdAt: string;
}

export interface CreateTenantRequest {
  name: string;
  contactPhone: string;
  contactName?: string | null;
  ownerPhone: string;
  ownerPassword: string;
  ownerRealName?: string | null;
  headquartersName: string;
  trialDays?: number | null;
}

export interface PlatformDashboard {
  totalTenants: number;
  activeTenants: number;
  trialTenants: number;
  expiredTenants: number;
  disabledTenants: number;
  expiringIn30Days: number;
  expiringIn7Days: number;
  revenueLast30Days: number;
  revenueThisYear: number;
  paidOrdersLast30Days: number;
  recentSubscriptions: RecentSubscription[];
}

export interface RecentSubscription {
  id: number;
  tenantId: number;
  tenantName: string;
  planName: string;
  amount: number;
  source: string;
  createdAt: string;
}

export interface OfflineActivateRequest {
  tenantId: number;
  planId: number;
  years: number;
  amountReceived: number;
  remark?: string | null;
}

export interface RevenueMonth {
  year: number;
  month: number;
  amount: number;
  orderCount: number;
}

export interface RevenueBreakdown {
  name: string;
  amount: number;
  orderCount: number;
}

export interface PlatformRevenue {
  months: number;
  totalAmount: number;
  totalOrders: number;
  newCustomerAmount: number;
  renewalAmount: number;
  monthlyTrend: RevenueMonth[];
  byPlan: RevenueBreakdown[];
  byChannel: RevenueBreakdown[];
}

export interface TenantTopTechnician {
  technicianId: number;
  name: string;
  roundCount: number;
  revenue: number;
}

export interface TenantOverview {
  tenantId: number;
  name: string;
  status: string;
  expireAt?: string | null;
  daysToExpire?: number | null;
  currentPlanName?: string | null;
  storeCount: number;
  activeStoreCount: number;
  staffCount: number;
  technicianCount: number;
  memberCount: number;
  revenue7Days: number;
  revenue30Days: number;
  orderCount30Days: number;
  topTechnicians: TenantTopTechnician[];
}

export interface PlatformSubscriptionSetting {
  expiryNotice?: string | null;
  contactPhone?: string | null;
  contactWechat?: string | null;
}
