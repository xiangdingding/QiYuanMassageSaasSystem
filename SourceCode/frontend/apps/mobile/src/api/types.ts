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
  balance: number;
  totalRecharge: number;
  totalConsumed: number;
  discount: number;
  remark?: string | null;
  level?: string | null;
  isActive: boolean;
  createdAt: string;
  memberTypeName?: string | null;
  memberTypeKind?: 'StoredValue' | 'CountBased' | null;
  totalCount?: number | null;
  remainCount?: number | null;
  cardExpiresAt?: string | null;
  cardDaysRemaining?: number | null;
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
