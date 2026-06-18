import { http } from './http';
import type {
  DailyReport,
  LoginRequest,
  LoginResponse,
  Member,
  MyQueue,
  PagedResult,
  QueueRow,
  Store,
  SubscriptionStatus,
  UserInfo
} from './types';

// 端点与 BS 端 api/modules.ts 完全一致，保证移动端与网页端同源同逻辑。
// 首批切片覆盖：登录 / 门店 / 会员 / 技师排队 / 日报 / 订阅。其余模块按迭代补充。

export interface UserProfile {
  id: number;
  username: string;
  realName?: string | null;
  phone?: string | null;
  role: string;
  tenantId?: number | null;
  storeId?: number | null;
}

export const authApi = {
  login: (req: LoginRequest) => http().post<LoginResponse>('/auth/login', req).then((r) => r.data),
  me: () => http().get<UserInfo>('/auth/me').then((r) => r.data),
  profile: () => http().get<UserProfile>('/auth/profile').then((r) => r.data),
  updateProfile: (body: { realName?: string | null; phone?: string | null }) =>
    http().put<UserProfile>('/auth/profile', body).then((r) => r.data),
  changePassword: (body: { oldPassword: string; newPassword: string }) =>
    http().post('/auth/change-password', body)
};

export const storesApi = {
  list: () => http().get<Store[]>('/stores').then((r) => r.data)
};

export interface MemberQuery {
  page?: number;
  pageSize?: number;
  keyword?: string;
  storeId?: number;
  includeClosed?: boolean;
}

export const membersApi = {
  list: (q: MemberQuery) => http().get<PagedResult<Member>>('/members', { params: q }).then((r) => r.data),
  get: (id: number) => http().get<Member>(`/members/${id}`).then((r) => r.data),
  rechargeHistory: (id: number) => http().get(`/members/${id}/recharges`).then((r) => r.data),
  consumptionHistory: (id: number) => http().get(`/members/${id}/orders`).then((r) => r.data)
};

export const queueApi = {
  list: (storeId: number) => http().get<QueueRow[]>('/queue', { params: { storeId } }).then((r) => r.data),
  setState: (technicianId: number, state: string) =>
    http().post(`/queue/${technicianId}/state`, { state }),
  callNext: (storeId: number) =>
    http().post<{ technicianId: number | null; technicianName: string | null; employeeNo: number | null; position: number }>(
      '/queue/call-next', { storeId }).then((r) => r.data),
  resetDay: (storeId: number) => http().post('/queue/reset-day', null, { params: { storeId } }),
  me: () => http().get<MyQueue>('/queue/me').then((r) => r.data),
  setMyState: (state: string) => http().post('/queue/me/state', { state })
};

export const reportsApi = {
  daily: (storeId: number, date?: string) =>
    http().get<DailyReport>('/reports/daily', { params: { storeId, date } }).then((r) => r.data)
};

export const subscriptionsApi = {
  me: () => http().get<SubscriptionStatus>('/subscriptions/me').then((r) => r.data)
};
