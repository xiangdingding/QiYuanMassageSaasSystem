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
  /** 营业日切日时间（分钟）。0=自然日；360=06:00 BJ。 */
  dayCloseCutoffMinutes: number;
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
  sort?: number;
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
  referredByStaffId?: number | null;
  referredByStaffName?: string | null;
  referralRewardEarned: number;
  createdAt: string;
  memberTypeId?: number | null;
  memberTypeName?: string | null;
  memberTypeKind?: 'StoredValue' | 'CountBased' | null;
  /** 次卡专属：累计购买次数（含赠送），非次卡为 null */
  totalCount?: number | null;
  /** 次卡专属：剩余次数，非次卡为 null */
  remainCount?: number | null;
  /** 次卡专属：会员类型模板绑定的服务项目 id；结账时用于校验购物车是否含该服务 */
  serviceItemId?: number | null;
  /** 次卡专属：绑定服务项目名称，用于"无匹配项目"提示 */
  serviceItemName?: string | null;
  /** 会员卡到期时间 = 开卡日 + 会员类型有效天数；null = 永久 */
  cardExpiresAt?: string | null;
  /** 距到期剩余天数（北京日历日）；负=已过期；null=永久 */
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
  /** 次卡核销时该字段非 null，UI 用于标识"次卡抵扣" */
  memberPackageId?: number | null;
  /** 服务面值单价（即使次卡核销也保留），用于小票面值列 */
  listUnitPrice?: number;
  /** 面值小计 = listUnitPrice × quantity */
  listAmount?: number;
  /** 技师上钟方式：Rotation 轮钟 / Designation 点钟 / Unknown 历史数据 */
  assignmentSource?: 'Rotation' | 'Designation' | 'Unknown';
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
  /** 面值合计：所有 item.listAmount 之和，次卡订单也会有非 0 值 */
  listTotal?: number;
  /** 走次卡核销的总次数 */
  punchCardUsedCount?: number;
  /** 会员手机（无会员订单为 null） */
  memberPhone?: string | null;
  /** 会员姓名 */
  memberName?: string | null;
  /** 会员卡类型名（"金卡 / 100次足疗卡"等） */
  memberTypeName?: string | null;
  /** 会员卡类型枚举 */
  memberTypeKind?: 'StoredValue' | 'CountBased' | null;
  /** 挂在该订单上的计时房费（与 items 并列展示，不计提成） */
  roomCharges?: OrderRoomCharge[] | null;
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
  /** 会员手机；非会员订单为 null */
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
    /** 上钟方式：Rotation 轮钟 / Designation 点钟；不传时后端兜底 Designation */
    assignmentSource?: 'Rotation' | 'Designation';
  }[];
  remark?: string | null;
  /** 一同结算的计时房 session id 列表（订单同步收尾这些房费，金额并入 Order.Total/PaidAmount） */
  roomSessionIds?: number[];
}

export interface CheckoutRequest {
  payMethod: string;
  paidAmount?: number | null;
  discountAmount?: number;
  remark?: string | null;
  /** 会员卡合并结算：同手机号下其它卡的 id 列表（PayMethod=MemberCard 时生效） */
  secondaryMemberIds?: number[];
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
  idCardNo?: string | null;
  birthDate?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  hireDate?: string | null;
  terminationDate?: string | null;
  specialties?: string | null;
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

/** 技师自助"我的班次"：自己的排队状态 + 当前上钟的房间/客户/上钟前必读。 */
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
  /** 适用来源：'Rotation' 仅轮钟 / 'Designation' 仅点钟 / null 通配两种 */
  assignmentSource?: 'Rotation' | 'Designation' | null;
  /** 轮钟专用金额（FixedAmount/Percentage 适用）。null = 回退到 amount */
  rotationAmount?: number | null;
  /** 点钟专用金额（FixedAmount/Percentage 适用）。null = 回退到 amount */
  designationAmount?: number | null;
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
  /** 点钟次数（AssignmentSource = Designation 的 Quantity 合计） */
  designationCount?: number;
  /** 轮钟次数（AssignmentSource = Rotation 的 Quantity 合计） */
  rotationCount?: number;
  /** 指定率 = designation / (designation + rotation)；分母 0 时为 null */
  designationRate?: number | null;
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
  isTimedRoom: boolean;
  hourlyRate: number;
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
  /** 门店当前的营业日切日时间（分钟）。 */
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
