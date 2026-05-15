export type UserRole = 'PlatformAdmin' | 'ShopOwner' | 'StoreManager' | 'Cashier' | 'Technician';

export interface UserInfo {
  id: number;
  username: string;
  realName?: string | null;
  role: UserRole;
  tenantId?: number | null;
  storeId?: number | null;
  isBlind: boolean;
}

export interface LoginRequest {
  username: string;
  password: string;
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

export interface Store {
  id: number;
  name: string;
  address?: string | null;
  phone?: string | null;
  isActive: boolean;
  isHeadquarters: boolean;
  parentStoreId?: number | null;
  createdAt: string;
}

export interface ServiceItem {
  id: number;
  code: string;
  name: string;
  durationMinutes: number;
  price: number;
  memberPrice: number;
  description?: string | null;
  isActive: boolean;
}

export interface Member {
  id: number;
  storeId: number;
  cardNo: string;
  phone: string;
  name?: string | null;
  gender?: string | null;
  birthday?: string | null;
  balance: number;
  totalRecharge: number;
  totalConsumed: number;
  discount: number;
  remark?: string | null;
  level?: string | null;
  preferenceNotes?: string | null;
  healthNotes?: string | null;
  isActive: boolean;
  closedAt?: string | null;
  closeReason?: string | null;
  referredByMemberId?: number | null;
  referredByMemberName?: string | null;
  referralRewardEarned: number;
  createdAt: string;
}

export interface OrderItem {
  id: number;
  serviceId: number;
  serviceName: string;
  technicianId: number;
  technicianName?: string | null;
  quantity: number;
  durationMinutes: number;
  unitPrice: number;
  itemTotal: number;
  commissionAmount: number;
  roomId?: number | null;
  roomNo?: string | null;
  previousTechnicianId?: number | null;
  transferredAt?: string | null;
}

export interface Order {
  id: number;
  orderNo: string;
  storeId: number;
  memberId?: number | null;
  memberCardNo?: string | null;
  total: number;
  discountAmount: number;
  paidAmount: number;
  payMethod: string;
  status: string;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  cashierUserId?: number | null;
  cashierName?: string | null;
  remark?: string | null;
  items: OrderItem[];
}

export interface OrderListItem {
  id: number;
  orderNo: string;
  total: number;
  paidAmount: number;
  payMethod: string;
  status: string;
  itemCount: number;
  createdAt: string;
  completedAt?: string | null;
  memberCardNo?: string | null;
}

export interface CreateOrderRequest {
  storeId: number;
  memberId?: number | null;
  items: { serviceId: number; technicianId: number; quantity?: number; roomId?: number | null }[];
  remark?: string | null;
}

export interface CheckoutRequest {
  payMethod: string;
  paidAmount?: number | null;
  discountAmount?: number;
  remark?: string | null;
}

export interface Staff {
  id: number;
  storeId?: number | null;
  username: string;
  realName?: string | null;
  phone?: string | null;
  role: UserRole;
  employeeNo?: number | null;
  isBlind: boolean;
  isActive: boolean;
  lastLoginAt?: string | null;
  createdAt: string;
}

export interface QueueRow {
  id: number;
  technicianId: number;
  technicianName: string;
  employeeNo?: number | null;
  state: 'Idle' | 'OnDuty' | 'Resting' | 'OffDuty';
  queuePosition: number;
  todayRoundCount: number;
  enteredAt?: string | null;
  lastCalledAt?: string | null;
}

export interface CommissionRule {
  id: number;
  serviceId?: number | null;
  serviceName?: string | null;
  technicianId?: number | null;
  technicianName?: string | null;
  ruleType: 'FixedAmount' | 'Percentage' | 'Tiered' | 'Timed';
  amount: number;
  tieredRulesJson?: string | null;
  priority: number;
  isActive: boolean;
}

export interface DailyReport {
  date: string;
  storeId: number;
  orderCount: number;
  revenue: number;
  cashAmount: number;
  memberCardAmount: number;
  wechatAmount: number;
  alipayAmount: number;
  bankCardAmount: number;
  refundCount: number;
  refundAmount: number;
  memberRechargeCount: number;
  memberRechargeAmount: number;
}

export interface TechnicianPerformance {
  technicianId: number;
  technicianName: string;
  employeeNo?: number | null;
  orderItemCount: number;
  totalServiceAmount: number;
  totalCommission: number;
  totalDurationMinutes: number;
}

export interface SubscriptionStatus {
  tenantId: number;
  status: string;
  expireAt?: string | null;
  daysToExpire?: number | null;
  currentPlanId?: number | null;
  currentPlanName?: string | null;
}

export type AppointmentStatus = 'Pending' | 'Confirmed' | 'Arrived' | 'Completed' | 'Cancelled' | 'NoShow';

export interface Appointment {
  id: number;
  storeId: number;
  storeName: string;
  serviceId?: number | null;
  serviceName?: string | null;
  preferredTechnicianId?: number | null;
  preferredTechnicianName?: string | null;
  customerName: string;
  customerPhone: string;
  expectedArriveAt: string;
  partySize: number;
  status: AppointmentStatus;
  remark?: string | null;
  createdAt: string;
  confirmedAt?: string | null;
  arrivedAt?: string | null;
  cancelledAt?: string | null;
  cancelReason?: string | null;
}

export interface Room {
  id: number;
  storeId: number;
  roomNo: string;
  capacity: number;
  roomType?: string | null;
  remark?: string | null;
  isActive: boolean;
  isOccupied: boolean;
  occupiedByOrderId?: number | null;
  occupiedByOrderNo?: string | null;
}

export interface DayClosePreview {
  businessDate: string;
  storeId: number;
  orderCount: number;
  revenueTotal: number;
  expectedCash: number;
  cashAmount: number;
  memberCardAmount: number;
  wechatAmount: number;
  alipayAmount: number;
  bankCardAmount: number;
  rechargeAmount: number;
  alreadyClosed: boolean;
}

export interface DayClose {
  id: number;
  storeId: number;
  businessDate: string;
  orderCount: number;
  revenueTotal: number;
  expectedCash: number;
  actualCash: number;
  variance: number;
  cashAmount: number;
  memberCardAmount: number;
  wechatAmount: number;
  alipayAmount: number;
  bankCardAmount: number;
  rechargeAmount: number;
  operatorUserId?: number | null;
  operatorName?: string | null;
  remark?: string | null;
  createdAt: string;
}
