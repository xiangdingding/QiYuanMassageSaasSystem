import { http } from './http';
import type {
  Appointment,
  AppointmentStatus,
  CheckoutRequest,
  CommissionRule,
  CreateOrderRequest,
  DailyReport,
  DayClose,
  DayClosePreview,
  LoginRequest,
  LoginResponse,
  Member,
  Order,
  OrderListItem,
  PagedResult,
  QueueRow,
  Room,
  ServiceItem,
  Staff,
  Store,
  SubscriptionStatus,
  TechnicianPerformance
} from './types';

export const authApi = {
  login: (req: LoginRequest) => http().post<LoginResponse>('/auth/login', req).then((r) => r.data),
  me: () => http().get('/auth/me').then((r) => r.data)
};

export const storesApi = {
  list: () => http().get<Store[]>('/stores').then((r) => r.data),
  create: (body: { name: string; address?: string | null; phone?: string | null; parentStoreId?: number | null }) =>
    http().post<Store>('/stores', body).then((r) => r.data),
  update: (id: number, body: { name: string; address?: string | null; phone?: string | null; isActive: boolean }) =>
    http().put<Store>(`/stores/${id}`, body).then((r) => r.data)
};

export const servicesApi = {
  list: (includeInactive = false) =>
    http().get<ServiceItem[]>('/services', { params: { includeInactive } }).then((r) => r.data),
  create: (body: Omit<ServiceItem, 'id'>) => http().post<ServiceItem>('/services', body).then((r) => r.data),
  update: (id: number, body: Omit<ServiceItem, 'id' | 'code'>) =>
    http().put<ServiceItem>(`/services/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/services/${id}`)
};

export interface MemberQuery {
  page?: number;
  pageSize?: number;
  keyword?: string;
  storeId?: number;
}

export const membersApi = {
  list: (q: MemberQuery) =>
    http().get<PagedResult<Member>>('/members', { params: q }).then((r) => r.data),
  get: (id: number) => http().get<Member>(`/members/${id}`).then((r) => r.data),
  create: (body: any) => http().post<Member>('/members', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<Member>(`/members/${id}`, body).then((r) => r.data),
  recharge: (body: { memberId: number; amount: number; bonusAmount: number; payMethod: string; remark?: string | null }) =>
    http().post('/members/recharge', body).then((r) => r.data),
  rechargeHistory: (id: number) => http().get(`/members/${id}/recharges`).then((r) => r.data),
  consumptionHistory: (id: number) => http().get(`/members/${id}/orders`).then((r) => r.data)
};

export const ordersApi = {
  list: (params: { page?: number; pageSize?: number; storeId?: number; status?: string; from?: string; to?: string }) =>
    http().get<PagedResult<OrderListItem>>('/orders', { params }).then((r) => r.data),
  get: (id: number) => http().get<Order>(`/orders/${id}`).then((r) => r.data),
  create: (body: CreateOrderRequest) => http().post<Order>('/orders', body).then((r) => r.data),
  checkout: (id: number, body: CheckoutRequest) => http().post<Order>(`/orders/${id}/checkout`, body).then((r) => r.data),
  refund: (id: number, reason?: string | null) => http().post<Order>(`/orders/${id}/refund`, { reason }).then((r) => r.data),
  cancel: (id: number) => http().post(`/orders/${id}/cancel`)
};

export const queueApi = {
  list: (storeId: number) => http().get<QueueRow[]>('/queue', { params: { storeId } }).then((r) => r.data),
  setState: (technicianId: number, state: string) =>
    http().post(`/queue/${technicianId}/state`, { state }),
  callNext: (storeId: number) =>
    http().post<{ technicianId: number | null; technicianName: string | null; employeeNo: number | null; position: number }>(
      '/queue/call-next', { storeId }).then((r) => r.data),
  resetDay: (storeId: number) => http().post('/queue/reset-day', null, { params: { storeId } })
};

export const staffApi = {
  list: (params: { page?: number; pageSize?: number; role?: string; storeId?: number; keyword?: string }) =>
    http().get<PagedResult<Staff>>('/staff', { params }).then((r) => r.data),
  create: (body: any) => http().post<Staff>('/staff', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<Staff>(`/staff/${id}`, body).then((r) => r.data),
  resetPassword: (id: number, newPassword: string) =>
    http().post(`/staff/${id}/reset-password`, { newPassword })
};

export const commissionsApi = {
  list: (serviceId?: number, technicianId?: number) =>
    http().get<CommissionRule[]>('/commission-rules', { params: { serviceId, technicianId } }).then((r) => r.data),
  create: (body: any) => http().post<CommissionRule>('/commission-rules', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<CommissionRule>(`/commission-rules/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/commission-rules/${id}`)
};

export const reportsApi = {
  daily: (storeId: number, date?: string) =>
    http().get<DailyReport>('/reports/daily', { params: { storeId, date } }).then((r) => r.data),
  technicianPerformance: (storeId: number, from: string, to: string) =>
    http().get<TechnicianPerformance[]>('/reports/technician-performance', { params: { storeId, from, to } }).then((r) => r.data)
};

export const subscriptionsApi = {
  me: () => http().get<SubscriptionStatus>('/subscriptions/me').then((r) => r.data)
};

export const appointmentsApi = {
  list: (params: { storeId?: number; status?: AppointmentStatus; from?: string; to?: string; page?: number; pageSize?: number }) =>
    http().get<PagedResult<Appointment>>('/appointments', { params }).then((r) => r.data),
  confirm: (id: number, remark?: string | null) =>
    http().post<Appointment>(`/appointments/${id}/confirm`, { remark }).then((r) => r.data),
  arrive: (id: number) => http().post<Appointment>(`/appointments/${id}/arrive`).then((r) => r.data),
  cancel: (id: number, reason?: string | null) =>
    http().post<Appointment>(`/appointments/${id}/cancel`, { reason }).then((r) => r.data)
};

export const roomsApi = {
  list: (storeId: number, includeInactive = false) =>
    http().get<Room[]>('/rooms', { params: { storeId, includeInactive } }).then((r) => r.data),
  create: (body: { storeId: number; roomNo: string; capacity: number; roomType?: string | null; remark?: string | null }) =>
    http().post<Room>('/rooms', body).then((r) => r.data),
  update: (id: number, body: { roomNo: string; capacity: number; roomType?: string | null; remark?: string | null; isActive: boolean }) =>
    http().put<Room>(`/rooms/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/rooms/${id}`)
};

export const dayClosesApi = {
  preview: (storeId: number, date?: string) =>
    http().get<DayClosePreview>('/day-closes/preview', { params: { storeId, date } }).then((r) => r.data),
  submit: (body: { storeId: number; businessDate: string; actualCash: number; remark?: string | null }) =>
    http().post<DayClose>('/day-closes', body).then((r) => r.data),
  history: (storeId: number, from?: string, to?: string) =>
    http().get<DayClose[]>('/day-closes', { params: { storeId, from, to } }).then((r) => r.data)
};

export const ordersTransferApi = {
  transfer: (orderId: number, itemId: number, body: { newTechnicianId: number; reason?: string | null }) =>
    http().patch<Order>(`/orders/${orderId}/items/${itemId}/transfer`, body).then((r) => r.data)
};
