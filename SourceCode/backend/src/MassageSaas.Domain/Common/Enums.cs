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
