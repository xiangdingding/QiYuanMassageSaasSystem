import { http } from './http';
import type {
  AppVersion,
  CreateAppVersionRequest,
  UpdateAppVersionRequest,
  Consultation,
  CreateTenantRequest,
  LoginRequest,
  LoginResponse,
  OfflineActivateRequest,
  PagedResult,
  Plan,
  PlatformAgreement,
  PlatformDashboard,
  PlatformManual,
  PlatformRevenue,
  PlatformSubscriptionSetting,
  ProcessConsultationRequest,
  UpdatePlatformAgreementRequest,
  UpdatePlatformManualRequest,
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

export interface ConsultationQuery {
  page?: number;
  pageSize?: number;
  status?: string;
  keyword?: string;
}

export const consultationsApi = {
  list: (q: ConsultationQuery) =>
    http().get<PagedResult<Consultation>>('/consultations', { params: q }).then((r) => r.data),
  pendingCount: () =>
    http().get<{ count: number }>('/consultations/pending-count').then((r) => r.data.count),
  process: (id: number, body: ProcessConsultationRequest) =>
    http().put<Consultation>(`/consultations/${id}/process`, body).then((r) => r.data)
};

export const appVersionsApi = {
  list: (platform?: string) =>
    http().get<AppVersion[]>('/app-versions', { params: platform ? { platform } : {} }).then((r) => r.data),
  create: (body: CreateAppVersionRequest) =>
    http().post<AppVersion>('/app-versions', body).then((r) => r.data),
  update: (id: number, body: UpdateAppVersionRequest) =>
    http().put<AppVersion>(`/app-versions/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/app-versions/${id}`).then((r) => r.data)
};

export const platformSettingsApi = {
  getSubscription: () =>
    http().get<PlatformSubscriptionSetting>('/platform-settings/subscription').then((r) => r.data),
  updateSubscription: (body: PlatformSubscriptionSetting) =>
    http().put<PlatformSubscriptionSetting>('/platform-settings/subscription', body).then((r) => r.data),
  getManual: () =>
    http().get<PlatformManual>('/platform-settings/manual', { params: { raw: true } }).then((r) => r.data),
  updateManual: (body: UpdatePlatformManualRequest) =>
    http().put<PlatformManual>('/platform-settings/manual', body).then((r) => r.data),
  getAgreements: () =>
    http().get<PlatformAgreement>('/platform-settings/agreements', { params: { raw: true } }).then((r) => r.data),
  updateAgreements: (body: UpdatePlatformAgreementRequest) =>
    http().put<PlatformAgreement>('/platform-settings/agreements', body).then((r) => r.data)
};
