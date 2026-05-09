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
  status: 'Active' | 'Expired' | 'Disabled';
  expireAt?: string | null;
  currentPlanId?: number | null;
  currentPlanName?: string | null;
  daysToExpire?: number | null;
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
  ownerUsername: string;
  ownerPassword: string;
  ownerRealName?: string | null;
  initialPlanId?: number | null;
  headquartersName: string;
}

export interface PlatformDashboard {
  totalTenants: number;
  activeTenants: number;
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
