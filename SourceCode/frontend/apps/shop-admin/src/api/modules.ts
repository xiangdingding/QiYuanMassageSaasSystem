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
  MemberPhoneGroup,
  PagedResult,
  QueueRow,
  Room,
  ServiceItem,
  Staff,
  Store,
  SubscriptionStatus,
  TechnicianPerformance
} from './types';

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
  me: () => http().get('/auth/me').then((r) => r.data),
  profile: () => http().get<UserProfile>('/auth/profile').then((r) => r.data),
  updateProfile: (body: { realName?: string | null; phone?: string | null }) =>
    http().put<UserProfile>('/auth/profile', body).then((r) => r.data),
  changePassword: (body: { oldPassword: string; newPassword: string }) =>
    http().post('/auth/change-password', body)
};

export interface RegisterTenantRequest {
  name: string;
  contactPhone: string;
  contactName?: string | null;
  ownerPhone: string;
  ownerPassword: string;
  ownerRealName?: string | null;
}

export interface RegisterTenantResponse {
  tenantId: number;
  tenantName: string;
  ownerPhone: string;
  expireAt: string;
  trialDays: number;
}

export const tenantsApi = {
  register: (req: RegisterTenantRequest) =>
    http().post<RegisterTenantResponse>('/tenants/register', req).then((r) => r.data)
};

export const storesApi = {
  list: () => http().get<Store[]>('/stores').then((r) => r.data),
  create: (body: { name: string; address?: string | null; phone?: string | null; parentStoreId?: number | null; dayCloseCutoffMinutes?: number }) =>
    http().post<Store>('/stores', body).then((r) => r.data),
  update: (id: number, body: { name: string; address?: string | null; phone?: string | null; isActive: boolean; dayCloseCutoffMinutes: number }) =>
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

export interface ReferralSummaryDto {
  referrerMemberId: number;
  referrerName: string;
  totalRewardEarned: number;
  referredCount: number;
  referredMembers: {
    memberId: number; cardNo: string; name: string | null;
    phone: string; totalRecharge: number; createdAt: string;
  }[];
}

export type MemberTypeKind = 'StoredValue' | 'CountBased';

export interface MemberType {
  id: number;
  code: string;
  name: string;
  sort: number;
  kind: MemberTypeKind;
  serviceItemId?: number | null;
  serviceItemName?: string | null;
  minRechargeAmount?: number | null;
  minPurchaseCount?: number | null;
  discount: number;
  bonusAmount?: number | null;
  bonusCount?: number | null;
  validDays?: number | null;
  isActive: boolean;
  remark?: string | null;
  createdAt: string;
}

export const memberTypesApi = {
  list: (includeInactive = false, kind?: MemberTypeKind) =>
    http().get<MemberType[]>('/member-types', { params: { includeInactive, kind } }).then((r) => r.data),
  create: (body: Partial<MemberType> & { kind: MemberTypeKind }) =>
    http().post<MemberType>('/member-types', body).then((r) => r.data),
  update: (id: number, body: Partial<MemberType>) =>
    http().put<MemberType>(`/member-types/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/member-types/${id}`)
};

export interface IssueCardResult {
  memberId: number;
  kind: MemberTypeKind;
  newBalance: number;
  paid: number;
  bonusAmount: number;
  bonusCount: number;
  memberPackageId?: number | null;
  expiresAt?: string | null;
}

export const membersApi = {
  list: (q: MemberQuery & { includeClosed?: boolean }) =>
    http().get<PagedResult<Member>>('/members', { params: q }).then((r) => r.data),
  grouped: (q: MemberQuery & { includeClosed?: boolean }) =>
    http().get<PagedResult<MemberPhoneGroup>>('/members/grouped', { params: q }).then((r) => r.data),
  get: (id: number) => http().get<Member>(`/members/${id}`).then((r) => r.data),
  create: (body: any) => http().post<Member>('/members', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<Member>(`/members/${id}`, body).then((r) => r.data),
  recharge: (body: { memberId: number; amount: number; bonusAmount: number; payMethod: string; remark?: string | null }) =>
    http().post('/members/recharge', body).then((r) => r.data),
  issueCard: (memberId: number, body: { memberTypeId: number; amount: number; count: number; payMethod: string; remark?: string | null }) =>
    http().post<IssueCardResult>(`/members/${memberId}/issue-card`, body).then((r) => r.data),
  rechargeHistory: (id: number) => http().get(`/members/${id}/recharges`).then((r) => r.data),
  consumptionHistory: (id: number) => http().get(`/members/${id}/orders`).then((r) => r.data),
  refund: (id: number, body: { refundAmount: number; refundMethod: string; reason?: string | null }) =>
    http().post(`/members/${id}/refund`, body).then((r) => r.data),
  transfer: (id: number, body: {
    toMemberId?: number | null;
    newMemberCardNo?: string | null;
    newMemberPhone?: string | null;
    newMemberName?: string | null;
    reason?: string | null;
  }) => http().post<Member>(`/members/${id}/transfer`, body).then((r) => r.data),
  referrals: (id: number) =>
    http().get<ReferralSummaryDto>(`/members/${id}/referrals`).then((r) => r.data)
};

export const ordersApi = {
  list: (params: { page?: number; pageSize?: number; storeId?: number; status?: string; from?: string; to?: string; keyword?: string }) =>
    http().get<PagedResult<OrderListItem>>('/orders', { params }).then((r) => r.data),
  get: (id: number) => http().get<Order>(`/orders/${id}`).then((r) => r.data),
  create: (body: CreateOrderRequest) => http().post<Order>('/orders', body).then((r) => r.data),
  checkout: (id: number, body: CheckoutRequest) => http().post<Order>(`/orders/${id}/checkout`, body).then((r) => r.data),
  refund: (id: number, reason?: string | null) => http().post<Order>(`/orders/${id}/refund`, { reason }).then((r) => r.data),
  cancel: (id: number) => http().post(`/orders/${id}/cancel`),
  itemsByTechnician: (storeId: number, technicianId: number, date: string) =>
    http().get<TechnicianServedItemDto[]>('/orders/items/by-technician',
      { params: { storeId, technicianId, date } }).then((r) => r.data)
};

export interface TechnicianServedItemDto {
  itemId: number;
  orderId: number;
  orderNo: string;
  serviceId: number;
  serviceName: string;
  completedAt: string | null;
  amount: number;
  memberId: number | null;
  memberName: string | null;
  memberCardNo: string | null;
  hasPendingComplaint: boolean;
  /** 该订单项是否已评价；代客录入评价时置灰 */
  hasReview?: boolean;
}

export const queueApi = {
  list: (storeId: number) => http().get<QueueRow[]>('/queue', { params: { storeId } }).then((r) => r.data),
  setState: (technicianId: number, state: string) =>
    http().post(`/queue/${technicianId}/state`, { state }),
  callNext: (storeId: number) =>
    http().post<{ technicianId: number | null; technicianName: string | null; employeeNo: number | null; position: number }>(
      '/queue/call-next', { storeId }).then((r) => r.data),
  resetDay: (storeId: number) => http().post('/queue/reset-day', null, { params: { storeId } })
};

export interface StaffTransferDto {
  id: number; userId: number; userName: string;
  fromStoreId: number; fromStoreName: string;
  toStoreId: number; toStoreName: string;
  kind: string; status: string;
  effectiveFrom: string; expectedReturnAt: string | null; returnedAt: string | null;
  reason: string | null; operatorName: string | null; createdAt: string;
}

export const staffApi = {
  list: (params: { page?: number; pageSize?: number; role?: string; storeId?: number; keyword?: string }) =>
    http().get<PagedResult<Staff>>('/staff', { params }).then((r) => r.data),
  create: (body: any) => http().post<Staff>('/staff', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<Staff>(`/staff/${id}`, body).then((r) => r.data),
  resetPassword: (id: number, newPassword: string) =>
    http().post(`/staff/${id}/reset-password`, { newPassword }),
  transfers: (params?: { userId?: number; storeId?: number; status?: string }) =>
    http().get<StaffTransferDto[]>('/staff/transfers', { params }).then((r) => r.data),
  transfer: (id: number, body: { toStoreId: number; kind: string; expectedReturnAt?: string | null; reason?: string | null }) =>
    http().post<StaffTransferDto>(`/staff/${id}/transfer`, body).then((r) => r.data),
  returnTransfer: (transferId: number) =>
    http().post<StaffTransferDto>(`/staff/transfers/${transferId}/return`).then((r) => r.data)
};

export const commissionsApi = {
  list: (serviceId?: number, technicianId?: number) =>
    http().get<CommissionRule[]>('/commission-rules', { params: { serviceId, technicianId } }).then((r) => r.data),
  create: (body: any) => http().post<CommissionRule>('/commission-rules', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<CommissionRule>(`/commission-rules/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/commission-rules/${id}`),
  bulk: (body: any) =>
    http().post<{ created: number; updated: number; skipped: number }>('/commission-rules/bulk', body).then((r) => r.data)
};

export interface MonthlyReportPoint { day: string; orderCount: number; revenue: number; rounds: number; }
export interface MonthlyReport {
  year: number; month: number; storeId: number;
  orderCount: number; revenue: number; rechargeAmount: number;
  roundsCount: number; averageOrder: number;
  daily: MonthlyReportPoint[];
}
export interface YearlyReport {
  year: number; storeId: number;
  orderCount: number; revenue: number; roundsCount: number;
  monthly: MonthlyReportPoint[];
}
export interface ServicePopularity { serviceId: number; serviceName: string; orderItemCount: number; roundsCount: number; revenue: number; }
export interface CustomerFlowPoint { date: string; orderCount: number; uniqueMembers: number; }

export interface MemberAnalysis {
  storeId: number; totalMembers: number; neverConsumed: number;
  activeMembers: number; dormantMembers: number; lostMembers: number;
  newMembersThisMonth: number; repeatMembers: number; repeatRate: number;
}
export interface ServiceTrendMonth { year: number; month: number; rounds: number; }
export interface ServiceTrend { serviceId: number; serviceName: string; totalRounds: number; months: ServiceTrendMonth[]; }
export interface ServicePopularityTrend { months: number; services: ServiceTrend[]; }
export interface TechnicianQuality {
  technicianId: number; technicianName: string; employeeNo: number | null;
  roundCount: number; complaintCount: number; complaintRate: number;
}

export const reportsApi = {
  daily: (storeId: number, date?: string) =>
    http().get<DailyReport>('/reports/daily', { params: { storeId, date } }).then((r) => r.data),
  technicianPerformance: (storeId: number, from: string, to: string) =>
    http().get<TechnicianPerformance[]>('/reports/technician-performance', { params: { storeId, from, to } }).then((r) => r.data),
  monthly: (storeId: number, year?: number, month?: number) =>
    http().get<MonthlyReport>('/reports/monthly', { params: { storeId, year, month } }).then((r) => r.data),
  yearly: (storeId: number, year?: number) =>
    http().get<YearlyReport>('/reports/yearly', { params: { storeId, year } }).then((r) => r.data),
  servicePopularity: (storeId: number, from: string, to: string) =>
    http().get<ServicePopularity[]>('/reports/service-popularity', { params: { storeId, from, to } }).then((r) => r.data),
  customerFlow: (storeId: number, from: string, to: string) =>
    http().get<CustomerFlowPoint[]>('/reports/customer-flow', { params: { storeId, from, to } }).then((r) => r.data),
  memberAnalysis: (storeId: number) =>
    http().get<MemberAnalysis>('/reports/member-analysis', { params: { storeId } }).then((r) => r.data),
  serviceTrend: (storeId: number, months: number) =>
    http().get<ServicePopularityTrend>('/reports/service-trend', { params: { storeId, months } }).then((r) => r.data),
  technicianQuality: (storeId: number, from: string, to: string) =>
    http().get<TechnicianQuality[]>('/reports/technician-quality', { params: { storeId, from, to } }).then((r) => r.data)
};

export interface SubscriptionPlan {
  id: number;
  code: string;
  name: string;
  maxStores: number;
  maxStaff: number;
  annualPrice: number;
  featureJson?: string | null;
  isActive: boolean;
}

export interface SubscriptionPaymentResp {
  paymentOrderId: number;
  orderNo: string;
  amount: number;
  channel: string;
  status: string;
  payUrl?: string | null;
  createdAt: string;
}

export const subscriptionsApi = {
  me: () => http().get<SubscriptionStatus>('/subscriptions/me').then((r) => r.data),
  plans: () => http().get<SubscriptionPlan[]>('/plans').then((r) => r.data),
  pay: (body: { tenantId: number; planId: number; years: number; channel: 'Wechat' | 'Alipay' }) =>
    http().post<SubscriptionPaymentResp>('/subscriptions/pay', body).then((r) => r.data),
  payStatus: (orderNo: string) =>
    http().get<SubscriptionPaymentResp>(`/subscriptions/pay/${orderNo}`).then((r) => r.data)
};

export const appointmentsApi = {
  list: (params: { storeId?: number; status?: AppointmentStatus; from?: string; to?: string; keyword?: string; page?: number; pageSize?: number }) =>
    http().get<PagedResult<Appointment>>('/appointments', { params }).then((r) => r.data),
  create: (body: {
    storeId: number;
    serviceId?: number | null;
    preferredTechnicianId?: number | null;
    customerName: string;
    customerPhone: string;
    expectedArriveAt: string;
    partySize: number;
    remark?: string | null;
  }) => http().post<Appointment>('/appointments', body).then((r) => r.data),
  update: (id: number, body: {
    serviceId?: number | null;
    preferredTechnicianId?: number | null;
    customerName: string;
    customerPhone: string;
    expectedArriveAt: string;
    partySize: number;
    remark?: string | null;
  }) => http().put<Appointment>(`/appointments/${id}`, body).then((r) => r.data),
  confirm: (id: number, remark?: string | null) =>
    http().post<Appointment>(`/appointments/${id}/confirm`, { remark }).then((r) => r.data),
  arrive: (id: number) => http().post<Appointment>(`/appointments/${id}/arrive`).then((r) => r.data),
  cancel: (id: number, reason?: string | null) =>
    http().post<Appointment>(`/appointments/${id}/cancel`, { reason }).then((r) => r.data)
};

export const roomsApi = {
  list: (storeId: number, includeInactive = false) =>
    http().get<Room[]>('/rooms', { params: { storeId, includeInactive } }).then((r) => r.data),
  create: (body: { storeId: number; roomNo: string; capacity: number; roomType?: string | null; remark?: string | null; isTimedRoom?: boolean; hourlyRate?: number }) =>
    http().post<Room>('/rooms', body).then((r) => r.data),
  update: (id: number, body: { roomNo: string; capacity: number; roomType?: string | null; remark?: string | null; isActive: boolean; isTimedRoom?: boolean; hourlyRate?: number }) =>
    http().put<Room>(`/rooms/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/rooms/${id}`)
};

export interface TimedRoomSessionDto {
  id: number; storeId: number; roomId: number; roomNo: string;
  memberId: number | null; memberName: string | null; customerName: string | null;
  startedAt: string; endedAt: string | null;
  hourlyRateSnapshot: number; billedMinutes: number; elapsedMinutes: number;
  amount: number; payMethod: string; status: string;
  operatorName: string | null; remark: string | null;
}

export const timedRoomsApi = {
  sessions: (storeId: number, params?: { status?: string; from?: string; to?: string }) =>
    http().get<TimedRoomSessionDto[]>('/timed-rooms/sessions', { params: { storeId, ...params } }).then((r) => r.data),
  start: (roomId: number, body: { memberId?: number | null; customerName?: string | null; remark?: string | null }) =>
    http().post<TimedRoomSessionDto>(`/timed-rooms/${roomId}/start`, body).then((r) => r.data),
  stop: (id: number, payMethod: string) =>
    http().post<TimedRoomSessionDto>(`/timed-rooms/sessions/${id}/stop`, { payMethod }).then((r) => r.data),
  cancel: (id: number) =>
    http().post<TimedRoomSessionDto>(`/timed-rooms/sessions/${id}/cancel`).then((r) => r.data)
};

export const dayClosesApi = {
  preview: (storeId: number, date?: string) =>
    http().get<DayClosePreview>('/day-closes/preview', { params: { storeId, date } }).then((r) => r.data),
  submit: (body: { storeId: number; businessDate: string; actualCash: number; remark?: string | null }) =>
    http().post<DayClose>('/day-closes', body).then((r) => r.data),
  history: (storeId: number, from?: string, to?: string) =>
    http().get<DayClose[]>('/day-closes', { params: { storeId, from, to } }).then((r) => r.data),
  revoke: (id: number, reason?: string | null) =>
    http().post<void>(`/day-closes/${id}/revoke`, { reason: reason || null }).then((r) => r.data)
};

export const ordersTransferApi = {
  transfer: (orderId: number, itemId: number, body: { newTechnicianId: number; reason?: string | null }) =>
    http().patch<Order>(`/orders/${orderId}/items/${itemId}/transfer`, body).then((r) => r.data),
  addItems: (orderId: number, items: any[]) =>
    http().post<Order>(`/orders/${orderId}/items`, { items }).then((r) => r.data),
  reopen: (orderId: number, reason?: string | null) =>
    http().post<Order>(`/orders/${orderId}/reopen`, { reason }).then((r) => r.data),
  setTip: (orderId: number, tipAmount: number) =>
    http().post<Order>(`/orders/${orderId}/tip`, { tipAmount }).then((r) => r.data)
};

export interface VoucherDto {
  id: number; kind: string; code: string; title: string;
  faceValue: number; minOrderAmount: number; discountPercent: number | null;
  validFrom: string | null; expiresAt: string | null; status: string;
  platform: string | null; remark: string | null;
  redeemedAt: string | null; redeemedOrderId: number | null; createdAt: string;
}

export const vouchersApi = {
  list: (params?: { status?: string; kind?: string; keyword?: string; page?: number; pageSize?: number }) =>
    http().get<{ items: VoucherDto[]; total: number; page: number; pageSize: number }>('/vouchers', { params }).then((r) => r.data),
  create: (body: Partial<VoucherDto>) => http().post<VoucherDto>('/vouchers', body).then((r) => r.data),
  /** 批量生成：服务端按 SC/GB 前缀生成 N 个无歧义码，返回 codes 数组 */
  batch: (body: {
    kind: string; count: number; title: string;
    faceValue: number; minOrderAmount: number; discountPercent: number | null;
    validFrom: string | null; expiresAt: string | null;
    platform: string | null; remark: string | null;
  }) => http().post<{ created: number; codes: string[] }>('/vouchers/batch', body).then((r) => r.data),
  /** 收银台 redeem 之前预览：按 code 精确取券详情，不改状态 */
  byCode: (code: string) =>
    http().get<VoucherDto>(`/vouchers/by-code/${encodeURIComponent(code)}`).then((r) => r.data),
  redeem: (body: { code: string; orderId: number }) =>
    http().post<VoucherDto>('/vouchers/redeem', body).then((r) => r.data),
  cancel: (id: number) => http().post(`/vouchers/${id}/cancel`),
  /** 批量作废：仅对 Active 生效；返回 { affected, skipped: [{ id, code, status, reason }] } */
  bulkCancel: (ids: number[]) =>
    http().post<{
      affected: number;
      skipped: { id: number; code: string | null; status: string; reason: string }[];
    }>('/vouchers/bulk-cancel', { ids }).then((r) => r.data),
  /** 批量删除（物理）：仅对 Cancelled 生效；其它状态会被跳过 */
  bulkDelete: (ids: number[]) =>
    http().post<{
      affected: number;
      skipped: { id: number; code: string | null; status: string; reason: string }[];
    }>('/vouchers/bulk-delete', { ids }).then((r) => r.data)
};

export interface InventoryItemDto {
  id: number; storeId: number; code: string; name: string; unit: string | null;
  quantity: number; minQuantity: number; unitCost: number | null;
  remark: string | null; isActive: boolean; lowStock: boolean;
}
export interface InventoryMovementDto {
  id: number; itemId: number; itemName: string; kind: string;
  delta: number; quantityAfter: number;
  operatorUserId: number | null; operatorName: string | null;
  remark: string | null; createdAt: string;
}

export const inventoryApi = {
  items: (storeId: number, onlyLowStock = false) =>
    http().get<InventoryItemDto[]>('/inventory/items', { params: { storeId, onlyLowStock } }).then((r) => r.data),
  createItem: (body: any) => http().post<InventoryItemDto>('/inventory/items', body).then((r) => r.data),
  updateItem: (id: number, body: any) => http().put<InventoryItemDto>(`/inventory/items/${id}`, body).then((r) => r.data),
  movements: (itemId: number, take = 50) =>
    http().get<InventoryMovementDto[]>('/inventory/movements', { params: { itemId, take } }).then((r) => r.data),
  move: (body: { itemId: number; kind: string; delta: number; remark?: string | null }) =>
    http().post<InventoryMovementDto>('/inventory/movements', body).then((r) => r.data)
};

export interface MemberPackageDto {
  id: number; memberId: number; memberName: string; storeId: number;
  kind: string; serviceId: number | null; serviceName: string | null;
  title: string; paidAmount: number; totalCount: number; remainCount: number;
  validFrom: string | null; expiresAt: string | null;
  status: string; remark: string | null; createdAt: string;
}

export const memberPackagesApi = {
  list: (params?: { memberId?: number; storeId?: number; status?: string }) =>
    http().get<MemberPackageDto[]>('/member-packages', { params }).then((r) => r.data),
  create: (body: any) => http().post<MemberPackageDto>('/member-packages', body).then((r) => r.data),
  cancel: (id: number) => http().post(`/member-packages/${id}/cancel`)
};

export interface ServicePackageDto {
  id: number; code: string; name: string; price: number; memberPrice: number | null;
  description: string | null; isActive: boolean;
  items: { serviceId: number; serviceName: string; quantity: number }[];
}

export const servicePackagesApi = {
  list: (includeInactive = false) =>
    http().get<ServicePackageDto[]>('/service-packages', { params: { includeInactive } }).then((r) => r.data),
  create: (body: any) => http().post<ServicePackageDto>('/service-packages', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<ServicePackageDto>(`/service-packages/${id}`, body).then((r) => r.data),
  remove: (id: number) => http().delete(`/service-packages/${id}`)
};

export interface ServiceReviewDto {
  id: number; orderId: number; orderItemId: number;
  technicianId: number; technicianName: string;
  memberId: number | null; memberName: string | null;
  rating: number; tags: string | null; comment: string | null; createdAt: string;
}

export const reviewsApi = {
  list: (params?: { technicianId?: number; rating?: number; from?: string; to?: string; page?: number; pageSize?: number }) =>
    http().get<{ items: ServiceReviewDto[]; total: number; page: number; pageSize: number }>('/reviews', { params }).then((r) => r.data),
  technicianSummary: (params?: { from?: string; to?: string }) =>
    http().get('/reviews/technician-summary', { params }).then((r) => r.data),
  me: () => http().get<ServiceReviewDto[]>('/reviews/me').then((r) => r.data),
  /** 门店端代客录入评价（已登录店员身份，无需 openId） */
  submit: (body: { orderId: number; orderItemId: number; rating: number; tags?: string | null; comment?: string | null }) =>
    http().post<ServiceReviewDto>('/reviews', body).then((r) => r.data)
};

export interface StaffScheduleDto {
  id: number; storeId: number; userId: number; userName: string;
  workDate: string; startTime: string; endTime: string; remark: string | null;
}
export interface LeaveRequestDto {
  id: number; userId: number; userName: string; type: string;
  fromDate: string; toDate: string; reason: string | null; status: string;
  approverUserId: number | null; approverName: string | null;
  approvedAt: string | null; createdAt: string;
}

export interface SalaryProfileDto {
  userId: number; userName: string;
  baseMonthly: number; overtimeHourRate: number;
  attendanceBonusAmount: number; requiredAttendanceDays: number;
  remark: string | null;
}
export interface PayrollAdjustmentDto {
  id: number; kind: string; amount: number; reason: string;
  operatorName: string | null; createdAt: string;
}
export interface PayrollItemDto {
  id: number; userId: number; userName: string; employeeNo: number | null;
  baseSalary: number; commissionTotal: number; tipsTotal: number;
  overtimeHours: number; overtimeAmount: number;
  attendanceBonus: number; adjustmentTotal: number; netTotal: number;
  servedRoundCount: number; scheduledDays: number; leaveDays: number;
  remark: string | null;
  adjustments: PayrollAdjustmentDto[];
}
export interface PayrollPeriodDto {
  id: number; storeId: number; year: number; month: number;
  status: string; generatedAt: string; lockedAt: string | null; paidAt: string | null;
  operatorName: string | null; totalAmount: number; itemCount: number; remark: string | null;
}
export interface PayrollPeriodDetailDto { period: PayrollPeriodDto; items: PayrollItemDto[]; }

export const payrollApi = {
  profiles: (storeId?: number) =>
    http().get<SalaryProfileDto[]>('/payroll/profiles', { params: { storeId } }).then((r) => r.data),
  profile: (userId: number) =>
    http().get<SalaryProfileDto>(`/payroll/profiles/${userId}`).then((r) => r.data),
  upsertProfile: (userId: number, body: Partial<SalaryProfileDto>) =>
    http().put<SalaryProfileDto>(`/payroll/profiles/${userId}`, body).then((r) => r.data),
  periods: (storeId: number, year?: number) =>
    http().get<PayrollPeriodDto[]>('/payroll/periods', { params: { storeId, year } }).then((r) => r.data),
  period: (id: number) =>
    http().get<PayrollPeriodDetailDto>(`/payroll/periods/${id}`).then((r) => r.data),
  generate: (storeId: number, year: number, month: number, remark?: string | null) =>
    http().post<PayrollPeriodDetailDto>('/payroll/periods', { storeId, year, month, remark }).then((r) => r.data),
  lock: (id: number, remark?: string | null) =>
    http().post<PayrollPeriodDto>(`/payroll/periods/${id}/lock`, { remark }).then((r) => r.data),
  markPaid: (id: number) =>
    http().post<PayrollPeriodDto>(`/payroll/periods/${id}/mark-paid`).then((r) => r.data),
  removeDraft: (id: number) => http().delete(`/payroll/periods/${id}`),
  updateItem: (id: number, overtimeHours: number, attendanceBonusOverride: number, remark?: string | null) =>
    http().patch<PayrollItemDto>(`/payroll/items/${id}`, { overtimeHours, attendanceBonusOverride, remark }).then((r) => r.data),
  addAdjustment: (itemId: number, kind: string, amount: number, reason: string) =>
    http().post<PayrollItemDto>(`/payroll/items/${itemId}/adjustments`, { kind, amount, reason }).then((r) => r.data),
  removeAdjustment: (itemId: number, adjId: number) =>
    http().delete<PayrollItemDto>(`/payroll/items/${itemId}/adjustments/${adjId}`).then((r) => r.data),
  me: (take = 6) => http().get<PayrollItemDto[]>('/payroll/me', { params: { take } }).then((r) => r.data)
};

export interface ComplaintDto {
  id: number; storeId: number;
  orderId: number | null; orderNo: string | null;
  orderItemId: number | null; serviceName: string | null;
  originalTechnicianId: number | null; originalTechnicianName: string | null;
  memberId: number | null; memberName: string | null;
  tags: string | null; comment: string | null;
  status: string;
  resolution: string | null;
  reassignedToTechnicianId: number | null;
  reassignedToTechnicianName: string | null;
  resolutionNote: string | null;
  recordedByName: string | null;
  resolvedByName: string | null;
  resolvedAt: string | null;
  createdAt: string;
}

export const complaintsApi = {
  list: (params: { storeId?: number; technicianId?: number; status?: string; from?: string; to?: string; page?: number; pageSize?: number }) =>
    http().get<PagedResult<ComplaintDto>>('/complaints', { params }).then((r) => r.data),
  get: (id: number) => http().get<ComplaintDto>(`/complaints/${id}`).then((r) => r.data),
  create: (body: { orderItemId?: number | null; storeId?: number | null; technicianId?: number | null; tags?: string | null; comment?: string | null }) =>
    http().post<ComplaintDto>('/complaints', body).then((r) => r.data),
  resolve: (id: number, body: { resolution: string; reassignedToTechnicianId?: number | null; resolutionNote?: string | null }) =>
    http().patch<ComplaintDto>(`/complaints/${id}/resolve`, body).then((r) => r.data),
  cancel: (id: number) => http().post<ComplaintDto>(`/complaints/${id}/cancel`).then((r) => r.data)
};

export const schedulesApi = {
  list: (storeId: number, from?: string, to?: string) =>
    http().get<StaffScheduleDto[]>('/schedules', { params: { storeId, from, to } }).then((r) => r.data),
  create: (body: any) => http().post<StaffScheduleDto>('/schedules', body).then((r) => r.data),
  remove: (id: number) => http().delete(`/schedules/${id}`),
  leaves: (params?: { userId?: number; status?: string }) =>
    http().get<LeaveRequestDto[]>('/schedules/leaves', { params }).then((r) => r.data),
  submitLeave: (body: any) => http().post<LeaveRequestDto>('/schedules/leaves', body).then((r) => r.data),
  approveLeave: (id: number, approve: boolean, reason?: string | null) =>
    http().post<LeaveRequestDto>(`/schedules/leaves/${id}/approve`, { approve, reason }).then((r) => r.data)
};
