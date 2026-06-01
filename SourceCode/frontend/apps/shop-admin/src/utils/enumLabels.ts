/**
 * 后端枚举到中文标签的集中映射。
 *
 * 规则：键是后端 enum 的字符串名（PascalCase，与 ToString() 一致），值是 UI 中文。
 * 任何 view 显示后端枚举字段时都应走这里，避免散落多处重复定义且容易漏掉。
 * 未命中时回退到原值，方便调试。
 */

function label(map: Record<string, string>) {
  return (raw: string | null | undefined): string => {
    if (!raw) return '';
    return map[raw] ?? raw;
  };
}

/// 订单 / 流水共用的支付方式
export const payMethodLabel = label({
  Cash: '现金',
  MemberCard: '会员卡',
  Wechat: '微信',
  Alipay: '支付宝',
  BankCard: '银行卡',
  Unpaid: '未支付'
});

/// 订单状态
export const orderStatusLabel = label({
  Pending: '待结账',
  InProgress: '服务中',
  Completed: '已完成',
  Cancelled: '已取消',
  Refunded: '已退款'
});

/// 优惠券状态
export const voucherStatusLabel = label({
  Active: '生效中',
  Redeemed: '已核销',
  Expired: '已过期',
  Cancelled: '已作废'
});

/// 优惠券类型
export const voucherKindLabel = label({
  GroupBuy: '团购券',
  StoreCoupon: '店内券'
});

/// 库存出入库类型
export const inventoryKindLabel = label({
  PurchaseIn: '采购入库',
  Consume: '服务消耗',
  Adjust: '盘点调整',
  Discard: '报损'
});

/// 工资单状态
export const payrollStatusLabel = label({
  Draft: '草稿',
  Locked: '已封盘',
  Paid: '已发放'
});

/// 工资调整项类型
export const payrollAdjustmentKindLabel = label({
  Bonus: '奖金',
  Deduction: '扣款'
});

/// 请假类型
export const leaveTypeLabel = label({
  Sick: '病假',
  Personal: '事假',
  Annual: '年假',
  Training: '培训'
});

/// 请假审批状态
export const leaveStatusLabel = label({
  Pending: '待审批',
  Approved: '已通过',
  Rejected: '已驳回',
  Cancelled: '已撤销'
});

/// 技师排班 / 在岗状态
export const queueStateLabel = label({
  OnDuty: '在岗',
  Resting: '休息',
  OffDuty: '下班',
  Idle: '空闲'
});

/// 角色
export const userRoleLabel = label({
  PlatformAdmin: '平台管理员',
  ShopOwner: '店主',
  StoreManager: '店长',
  Cashier: '收银员',
  Technician: '技师'
});

/// 上钟方式：轮钟 / 点钟
export const assignmentSourceLabel = label({
  Rotation: '轮钟',
  Designation: '点钟'
});

/// 会员卡类型
export const memberTypeKindLabel = label({
  StoredValue: '充值卡',
  CountBased: '次卡'
});

/// 会员资金流水类型
export const memberRechargeKindLabel = label({
  Recharge: '充值',
  Refund: '退款',
  Transfer: '转账',
  TransferIn: '转入',
  TransferOut: '转出',
  Consume: '消费',
  Bonus: '赠送'
});

/// 服务评价相关
export const reviewStatusLabel = label({
  Pending: '待回复',
  Resolved: '已回复'
});

/// 投诉处理状态
export const complaintStatusLabel = label({
  Open: '待处理',
  Resolved: '已处理',
  Closed: '已关闭'
});

/// 会员状态
export const memberStatusLabel = label({
  Active: '生效中',
  Inactive: '已关闭',
  Frozen: '已冻结'
});

/// 订阅状态
export const subscriptionStatusLabel = label({
  Trial: '试用中',
  Active: '生效中',
  Expired: '已到期',
  Disabled: '已停用'
});
