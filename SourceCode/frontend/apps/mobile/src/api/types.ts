// 与 BS 端 api/types.ts 同源（移动端首批切片所需子集）。新增模块时按需补充。
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
  dayCloseCutoffMinutes: number;
  createdAt: string;
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
  isActive: boolean;
  referredByMemberId?: number | null;
  referredByMemberName?: string | null;
  referredByStaffId?: number | null;
  referredByStaffName?: string | null;
  createdAt: string;
  memberTypeId?: number | null;
  memberTypeName?: string | null;
  memberTypeKind?: 'StoredValue' | 'CountBased' | null;
  totalCount?: number | null;
  remainCount?: number | null;
  /** 次卡绑定的服务项目 id；结账时校验购物车是否含该服务 */
  serviceItemId?: number | null;
  serviceItemName?: string | null;
  cardExpiresAt?: string | null;
  cardDaysRemaining?: number | null;
}

export interface MemberPhoneGroup {
  phone: string;
  primaryName?: string | null;
  cardCount: number;
  totalBalance: number;
  totalRecharge: number;
  totalConsumed: number;
  hasInactive: boolean;
  cards: Member[];
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
  dayCloseCutoffMinutes: number;
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

export interface MyQueue {
  id: number | null;
  technicianId: number;
  state: 'Idle' | 'OnDuty' | 'Resting' | 'OffDuty';
  queuePosition: number;
  todayRoundCount: number;
  enteredAt?: string | null;
  lastCalledAt?: string | null;
  currentRoomNo?: string | null;
  currentOrderId?: number | null;
  currentServiceName?: string | null;
  currentCustomerName?: string | null;
  currentCustomerGender?: string | null;
  currentCustomerPreferences?: string | null;
  currentCustomerHealth?: string | null;
  currentCustomerHasNotes: boolean;
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

export interface SubscriptionStatus {
  tenantId: number;
  status: string;
  expireAt?: string | null;
  daysToExpire?: number | null;
  currentPlanId?: number | null;
  currentPlanName?: string | null;
}

export const ROLE_LABELS: Record<UserRole, string> = {
  PlatformAdmin: '平台管理员',
  ShopOwner: '店主',
  StoreManager: '店长',
  Cashier: '收银员',
  Technician: '技师'
};

export const QUEUE_STATE_LABELS: Record<QueueRow['state'], string> = {
  Idle: '空闲',
  OnDuty: '上钟中',
  Resting: '休息',
  OffDuty: '下班'
};

// ---- 以下为「订单 / 预约 / 房间 / 报表」模块所需实体（与 BS 端 api/types.ts 同源子集）----

export interface ServiceItem {
  id: number;
  code: string;
  name: string;
  durationMinutes: number;
  price: number;
  memberPrice: number;
  description?: string | null;
  isActive: boolean;
  sort?: number;
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
  idCardNo?: string | null;
  birthDate?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  hireDate?: string | null;
  terminationDate?: string | null;
  specialties?: string | null;
}

/** 平台级"服务订阅"展示配置（平台端维护，本端只读）。 */
export interface PlatformSubscriptionSetting {
  expiryNotice?: string | null;
  contactPhone?: string | null;
  contactWechat?: string | null;
}

/** 平台级用户使用说明书：CS / BS × 正常 / 无障碍 共四份（平台端维护，本端只读，帮助展示）。 */
export interface PlatformManual {
  csManualNormal: string;
  csManualA11y: string;
  bsManualNormal: string;
  bsManualA11y: string;
}

/** 平台级注册协议：《用户服务协议》+《隐私协议》（平台端维护，注册页匿名只读）。 */
export interface PlatformAgreement {
  serviceAgreement: string;
  privacyPolicy: string;
}

export interface OrderRoomCharge {
  sessionId: number;
  roomId: number;
  roomNo: string;
  minutes: number;
  hourlyRate: number;
  amount: number;
  payMethod: string;
  status: string;
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
  memberPackageId?: number | null;
  listUnitPrice?: number;
  listAmount?: number;
  assignmentSource?: 'Rotation' | 'Designation' | 'Unknown';
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
  listTotal?: number;
  punchCardUsedCount?: number;
  memberPhone?: string | null;
  memberName?: string | null;
  memberTypeName?: string | null;
  memberTypeKind?: 'StoredValue' | 'CountBased' | null;
  roomCharges?: OrderRoomCharge[] | null;
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
  memberPhone?: string | null;
}

export interface CreateOrderRequest {
  storeId: number;
  memberId?: number | null;
  items: {
    serviceId: number;
    technicianId: number;
    quantity?: number;
    roomId?: number | null;
    assignmentSource?: 'Rotation' | 'Designation';
  }[];
  remark?: string | null;
  roomSessionIds?: number[];
}

export interface CheckoutRequest {
  payMethod: string;
  paidAmount?: number | null;
  discountAmount?: number;
  remark?: string | null;
  secondaryMemberIds?: number[];
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
  isTimedRoom: boolean;
  hourlyRate: number;
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
  assignmentSource?: 'Rotation' | 'Designation' | null;
  rotationAmount?: number | null;
  designationAmount?: number | null;
}

export interface TechnicianPerformance {
  technicianId: number;
  technicianName: string;
  employeeNo?: number | null;
  orderItemCount: number;
  totalServiceAmount: number;
  totalCommission: number;
  totalDurationMinutes: number;
  designationCount?: number;
  rotationCount?: number;
  designationRate?: number | null;
}

// 订单状态 / 支付方式 / 预约状态 中文映射（与 BS 端一致）
export const ORDER_STATUS_LABELS: Record<string, string> = {
  Pending: '待结账',
  InProgress: '服务中',
  Completed: '已完成',
  Cancelled: '已取消',
  Refunded: '已退款'
};

export const PAY_METHOD_LABELS: Record<string, string> = {
  Cash: '现金',
  MemberCard: '会员卡',
  Wechat: '微信',
  Alipay: '支付宝',
  BankCard: '银行卡',
  Unpaid: '未支付'
};

export const APPOINTMENT_STATUS_LABELS: Record<AppointmentStatus, string> = {
  Pending: '待确认',
  Confirmed: '已确认',
  Arrived: '已到店',
  Completed: '已完成',
  Cancelled: '已取消',
  NoShow: '未到店'
};
