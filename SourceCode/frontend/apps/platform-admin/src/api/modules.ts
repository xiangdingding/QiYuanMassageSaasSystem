import { http } from './http';
import type {
  CreateTenantRequest,
  LoginRequest,
  LoginResponse,
  OfflineActivateRequest,
  PagedResult,
  Plan,
  PlatformDashboard,
  PlatformRevenue,
  PlatformSubscriptionSetting,
  TenantDetail,
  TenantOverview,
  TenantSummary
} from './types';

export const authApi = {
  login: (req: LoginRequest) => http().post<LoginResponse>('/auth/login', req).then((r) => r.data)
};

export const plansApi = {
  list: (includeInactive = false) =>
    http().get<Plan[]>('/plans', { params: { includeInactive } }).then((r) => r.data),
  create: (body: Omit<Plan, 'id'>) => http().post<Plan>('/plans', body).then((r) => r.data),
  update: (id: number, body: Omit<Plan, 'id' | 'code'>) =>
    http().put<Plan>(`/plans/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/plans/${id}`)
};

export interface TenantQuery {
  page?: number;
  pageSize?: number;
  keyword?: string;
  status?: string;
}

export const tenantsApi = {
  list: (q: TenantQuery) =>
    http().get<PagedResult<TenantSummary>>('/tenants', { params: q }).then((r) => r.data),
  get: (id: number) => http().get<TenantDetail>(`/tenants/${id}`).then((r) => r.data),
  overview: (id: number) =>
    http().get<TenantOverview>(`/tenants/${id}/overview`).then((r) => r.data),
  create: (body: CreateTenantRequest) =>
    http().post<TenantDetail>('/tenants', body).then((r) => r.data),
  updateStatus: (id: number, status: string) =>
    http().patch(`/tenants/${id}/status`, { status }),
  remove: (id: number) => http().delete(`/tenants/${id}`)
};

export const dashboardApi = {
  platform: () => http().get<PlatformDashboard>('/dashboard/platform').then((r) => r.data),
  revenue: (months: number) =>
    http().get<PlatformRevenue>('/dashboard/platform/revenue', { params: { months } }).then((r) => r.data)
};

export const subscriptionsApi = {
  activateOffline: (req: OfflineActivateRequest) =>
    http().post('/subscriptions/activate-offline', req).then((r) => r.data),
  history: (tenantId: number) =>
    http().get(`/subscriptions/history`, { params: { tenantId } }).then((r) => r.data)
};

export const platformSettingsApi = {
  getSubscription: () =>
    http().get<PlatformSubscriptionSetting>('/platform-settings/subscription').then((r) => r.data),
  updateSubscription: (body: PlatformSubscriptionSetting) =>
    http().put<PlatformSubscriptionSetting>('/platform-settings/subscription', body).then((r) => r.data)
};
