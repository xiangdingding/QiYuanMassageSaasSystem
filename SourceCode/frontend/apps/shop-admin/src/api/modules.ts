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

export const membersApi = {
  list: (q: MemberQuery & { includeClosed?: boolean }) =>
    http().get<PagedResult<Member>>('/members', { params: q }).then((r) => r.data),
  get: (id: number) => http().get<Member>(`/members/${id}`).then((r) => r.data),
  create: (body: any) => http().post<Member>('/members', body).then((r) => r.data),
  update: (id: number, body: any) => http().put<Member>(`/members/${id}`, body).then((r) => r.data),
  recharge: (body: { memberId: number; amount: number; bonusAmount: number; payMethod: string; remark?: string | null }) =>
    http().post('/members/recharge', body).then((r) => r.data),
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
  remove: (id: number) => http().delete(`/commission-rules/${id}`)
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
    http().get<CustomerFlowPoint[]>('/reports/customer-flow', { params: { storeId, from, to } }).then((r) => r.data)
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
    http().get<DayClose[]>('/day-closes', { params: { storeId, from, to } }).then((r) => r.data)
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
  list: (params?: { status?: string; keyword?: string }) =>
    http().get<VoucherDto[]>('/vouchers', { params }).then((r) => r.data),
  create: (body: Partial<VoucherDto>) => http().post<VoucherDto>('/vouchers', body).then((r) => r.data),
  redeem: (body: { code: string; orderId: number }) =>
    http().post<VoucherDto>('/vouchers/redeem', body).then((r) => r.data),
  cancel: (id: number) => http().post(`/vouchers/${id}/cancel`)
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
  list: (params?: { technicianId?: number; rating?: number; from?: string; to?: string }) =>
    http().get<ServiceReviewDto[]>('/reviews', { params }).then((r) => r.data),
  technicianSummary: (params?: { from?: string; to?: string }) =>
    http().get('/reviews/technician-summary', { params }).then((r) => r.data),
  me: () => http().get<ServiceReviewDto[]>('/reviews/me').then((r) => r.data)
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
  orderId: number; orderNo: string;
  orderItemId: number; serviceName: string;
  originalTechnicianId: number; originalTechnicianName: string;
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
  create: (body: { orderItemId: number; tags?: string | null; comment?: string | null }) =>
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
