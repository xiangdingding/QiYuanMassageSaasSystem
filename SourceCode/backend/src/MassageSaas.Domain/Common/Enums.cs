namespace MassageSaas.Domain.Common;

public enum TenantStatus
{
    Active = 0,
    Expired = 1,
    Disabled = 2
}

public enum UserRole
{
    PlatformAdmin = 0,
    ShopOwner = 10,
    StoreManager = 20,
    Cashier = 30,
    Technician = 40
}

public enum OrderStatus
{
    Pending = 0,
    InProgress = 10,
    Completed = 20,
    Cancelled = 90,
    Refunded = 91
}

public enum PayMethod
{
    Unpaid = 0,
    Cash = 1,
    MemberCard = 2,
    Wechat = 3,
    Alipay = 4,
    BankCard = 5
}

public enum QueueState
{
    Idle = 0,
    OnDuty = 10,
    Resting = 20,
    OffDuty = 30
}

public enum CommissionRuleType
{
    FixedAmount = 0,
    Percentage = 1,
    Tiered = 2,
    Timed = 3
}

public enum PaymentChannel
{
    Wechat = 0,
    Alipay = 1,
    Offline = 9
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 10,
    Failed = 20,
    Refunded = 30,
    Cancelled = 40
}

public enum SubscriptionSource
{
    OnlinePayment = 0,
    OfflineManual = 1
}

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 10,
    Arrived = 20,
    Completed = 30,
    Cancelled = 90,
    NoShow = 91
}

/// <summary>技师等级。盲人按摩行业按"师傅"分级定价：初级=新手，中级=主力，高级=老师傅。</summary>
public enum TechnicianLevel
{
    Junior = 0,    // 初级
    Senior = 10,   // 中级（默认）
    Master = 20    // 高级 / 老师傅
}

public enum MemberLevel
{
    Regular = 0,   // 普通卡
    Silver = 10,   // 银卡
    Gold = 20,     // 金卡
    Diamond = 30   // 钻石
}

/// <summary>会员卡套餐类型：计次（按次数）或期限（按时间到期日）。</summary>
public enum MemberPackageKind
{
    Counter = 0,   // 计次卡
    Period = 10    // 期限卡（月/季/年）
}

public enum MemberPackageStatus
{
    Active = 0,
    Used = 10,
    Expired = 20,
    Cancelled = 90
}

public enum VoucherKind
{
    GroupBuy = 0,   // 团购券（美团/点评等外部平台券码）
    StoreCoupon = 10 // 店内优惠券（满减/折扣）
}

public enum VoucherStatus
{
    Active = 0,
    Redeemed = 10,
    Expired = 20,
    Cancelled = 90
}

/// <summary>排班/请假常用类型，盲人按摩店里常用的几种。</summary>
public enum LeaveType
{
    Sick = 0,      // 病假
    Personal = 10, // 事假
    Annual = 20,   // 年假
    Training = 30  // 培训
}

public enum LeaveStatus
{
    Pending = 0,
    Approved = 10,
    Rejected = 20,
    Cancelled = 90
}

public enum InventoryMovementKind
{
    PurchaseIn = 0,  // 采购入库
    Consume = 10,    // 服务消耗
    Adjust = 20,     // 盘点调整
    Discard = 30     // 报损
}
